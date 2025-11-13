using FileTransferTool.Infrastructure.Constants;

namespace FileTransferTool.Infrastructure.Services
{
    /// <summary>
    /// Handles writing file blocks to the destination file.
    /// </summary>
    public class FileWriter
    {
        public async Task WriteBlockAsync(
            string filePath,
            long offset,
            byte[] data,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is required", nameof(filePath));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (data.Length == 0) return;

            // Destination file MUST be pre-allocated once by the caller (FileTransferUseCase).
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Destination file not found: {filePath}. Ensure pre-allocation.");

            // Allow concurrent readers/writers; use async IO and consistent buffer size.
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite,
                IOConstants.FileStreamBufferSize,
                FileOptions.Asynchronous);

            stream.Seek(offset, SeekOrigin.Begin);
            await stream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
