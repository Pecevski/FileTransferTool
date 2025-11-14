using FileTransferTool.Application.DTOs;
using FileTransferTool.Application.Interfaces;
using FileTransferTool.Domain.Entities;
using FileTransferTool.Domain.Enums;
using FileTransferTool.Domain.Interfaces;

namespace FileTransferTool.Application.Services
{
    /// <summary>
    /// Orchestrates the file transfer use case with block division, parallel processing, and verification.
    /// </summary>
    public class FileTransferUseCase : IFileTransferUseCase
    {
        private const long BLOCK_SIZE = 1024 * 1024; // 1MB
        private const int MAX_RETRIES = 3;
        private const int DEFAULT_THREAD_COUNT = 2;

        private readonly IBlockTransferService _blockTransferService;
        private readonly IHashCalculator _blockHashCalculator;
        private readonly IHashCalculator _fileHashCalculator;
        private readonly IProgressReporter _progressReporter;

        public FileTransferUseCase(
            IBlockTransferService blockTransferService,
            IHashCalculator blockHashCalculator,
            IHashCalculator fileHashCalculator,
            IProgressReporter progressReporter)
        {
            _blockTransferService = blockTransferService ?? throw new ArgumentNullException(nameof(blockTransferService));
            _blockHashCalculator = blockHashCalculator ?? throw new ArgumentNullException(nameof(blockHashCalculator));
            _fileHashCalculator = fileHashCalculator ?? throw new ArgumentNullException(nameof(fileHashCalculator));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
        }

        /// <summary>
        /// Execute the file transfer process.
        /// </summary>
        public async Task<FileTransferResult> ExecuteAsync(
            string sourceFile,
            string destinationPathOrFile,
            int threadCount = DEFAULT_THREAD_COUNT,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException($"Source file not found: {sourceFile}");

            var startTime = DateTime.UtcNow;
            var fileInfo = new FileInfo(sourceFile);
            var blocks = DivideFileIntoBlocks(fileInfo.Length);

            _progressReporter.ReportTransferStarted(fileInfo.Length);

            // Resolve destination path:
            var destinationFile = ResolveDestinationFile(sourceFile, destinationPathOrFile);

            // Ensure destination directory exists and is writable
            var destDirectory = Path.GetDirectoryName(destinationFile) ?? Directory.GetCurrentDirectory();
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            await EnsureDirectoryWritableAsync(destDirectory, cancellationToken);

            // Pre-allocate the destination file to avoid concurrent resize/race conditions
            try
            {
                using (var destStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 81920, useAsync: true))
                {
                    destStream.SetLength(fileInfo.Length);
                    await destStream.FlushAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Error creating destination file '{destinationFile}': {ex.Message}", ex);
            }

            // Transfer all blocks with parallel processing
            var coordinator = new BlockCopyCoordinator(
                _blockTransferService,
                _blockHashCalculator,
                _progressReporter,
                MAX_RETRIES);

            var transferredBlocks = await coordinator.ProcessBlocksAsync(
                sourceFile,
                destinationFile,
                blocks,
                threadCount,
                cancellationToken);

            // Compute and verify final file hashes
            var sourceFileHash = await _fileHashCalculator.ComputeHashAsync(sourceFile, 0, fileInfo.Length);
            var destFileHash = await _fileHashCalculator.ComputeHashAsync(destinationFile, 0, fileInfo.Length);

            _progressReporter.ReportFinalFileHashes(
                Convert.ToHexString(sourceFileHash).ToLower(),
                Convert.ToHexString(destFileHash).ToLower(),
                _fileHashCalculator.AlgorithmName);

            var duration = DateTime.UtcNow - startTime;
            _progressReporter.ReportTransferCompleted(fileInfo.Length, duration);

            return new FileTransferResult
            {
                SourceFile = sourceFile,
                DestinationFile = destinationFile,
                TotalBytes = fileInfo.Length,
                BlockCount = blocks.Count,
                SuccessfulBlocks = transferredBlocks.Count(b => b.Status == BlockTransferStatus.Completed),
                FailedBlocks = transferredBlocks.Count(b => b.Status == BlockTransferStatus.Failed),
                SourceFileHash = sourceFileHash,
                DestinationFileHash = destFileHash,
                HashAlgorithm = _fileHashCalculator.AlgorithmName,
                Duration = duration,
                Blocks = transferredBlocks
            };
        }

        private List<FileBlock> DivideFileIntoBlocks(long fileSize)
        {
            var blocks = new List<FileBlock>();
            long offset = 0;
            int blockNumber = 0;

            while (offset < fileSize)
            {
                var blockSize = Math.Min(BLOCK_SIZE, fileSize - offset);
                blocks.Add(new FileBlock
                {
                    BlockNumber = blockNumber,
                    Offset = offset,
                    Size = blockSize,
                    Status = BlockTransferStatus.Pending,
                    RetryCount = 0
                });
                offset += blockSize;
                blockNumber++;
            }

            return blocks;
        }

        /// <summary>
        /// Resolve the destination file path. If the provided path is a directory (existing or ends with a separator),
        /// or appears to be a directory (no extension and parent doesn't exist), combine it with the source file name.
        /// Otherwise treat the value as a file path and ensure parent exists.
        /// </summary>
        private string ResolveDestinationFile(string sourceFile, string destinationInput)
        {
            if (string.IsNullOrWhiteSpace(destinationInput))
                throw new ArgumentException("Destination path cannot be empty", nameof(destinationInput));

            var dest = destinationInput.Trim();

            // If input is an existing directory -> use it
            if (Directory.Exists(dest))
                return Path.Combine(dest, Path.GetFileName(sourceFile));

            // If ends with directory separator -> treat as directory
            if (dest.EndsWith(Path.DirectorySeparatorChar) || dest.EndsWith(Path.AltDirectorySeparatorChar))
            {
                Directory.CreateDirectory(dest);
                return Path.Combine(dest, Path.GetFileName(sourceFile));
            }

            // If user passed drive root like "C:" treat as directory
            if (dest.Length == 2 && dest[1] == Path.VolumeSeparatorChar)
            {
                dest += Path.DirectorySeparatorChar;
                Directory.CreateDirectory(dest);
                return Path.Combine(dest, Path.GetFileName(sourceFile));
            }

            // At this point, assume dest is a file path. Create parent directory if necessary.
            var parent = Path.GetDirectoryName(dest);
            if (string.IsNullOrEmpty(parent))
            {
                // No parent => relative path, combine with current directory
                parent = Directory.GetCurrentDirectory();
                dest = Path.Combine(parent, dest);
                return dest;
            }

            if (!Directory.Exists(parent))
            {
                // Heuristic: if destination has no extension, user likely meant a directory
                if (string.IsNullOrEmpty(Path.GetExtension(dest)))
                {
                    Directory.CreateDirectory(dest);
                    return Path.Combine(dest, Path.GetFileName(sourceFile));
                }

                // Otherwise create parent directories for the target file
                Directory.CreateDirectory(parent);
            }

            return dest;
        }

        /// <summary>
        /// Verify that the directory is writable by attempting to create and delete a small temp file.
        /// Throws UnauthorizedAccessException if write is not allowed.
        /// </summary>
        private static async Task EnsureDirectoryWritableAsync(string directory, CancellationToken cancellationToken)
        {
            var testFile = Path.Combine(directory, $".ftt_write_test_{Guid.NewGuid():N}.tmp");
            try
            {
                // Use async file creation to exercise same I/O pipeline
                await using (var fs = new FileStream(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    var buffer = Array.Empty<byte>();
                    await fs.WriteAsync(buffer, cancellationToken);
                    await fs.FlushAsync(cancellationToken);
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"Permission denied: Cannot write to '{directory}'. Please check folder permissions.");
            }
            catch (Exception ex) when (ex is IOException || ex is NotSupportedException)
            {
                throw new IOException($"Directory '{directory}' is not writable: {ex.Message}", ex);
            }
            finally
            {
                try { if (File.Exists(testFile)) File.Delete(testFile); } catch { /* ignore cleanup errors */ }
            }
        }
    }
}
