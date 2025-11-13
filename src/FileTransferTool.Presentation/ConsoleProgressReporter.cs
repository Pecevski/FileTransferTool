using FileTransferTool.Domain.Entities;
using FileTransferTool.Domain.Interfaces;
using System.Text;

namespace FileTransferTool.Presentation
{
    /// <summary>
    /// Implements progress reporting to the console.
    /// </summary>
    public class ConsoleProgressReporter : IProgressReporter
    {
        private readonly object _lockObject = new();

        public void ReportBlockStarted(int blockNumber, long size)
        {
            lock (_lockObject)
            {
                Console.WriteLine($"[BLOCK {blockNumber}] Starting transfer ({FormatBytes(size)})");
            }
        }

        public void ReportBlockCompleted(FileBlock block)
        {
            lock (_lockObject)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[BLOCK {block.BlockNumber}] ✓ Completed - Hash: {ToHexString(block.SourceHash).ToLower().Substring(0, 16)}...");
                Console.ResetColor();
            }
        }

        public void ReportBlockFailed(FileBlock block, string error)
        {
            lock (_lockObject)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[BLOCK {block.BlockNumber}] ✗ Failed - {error}");
                Console.ResetColor();
            }
        }

        public void ReportBlockRetry(FileBlock block, int retryCount)
        {
            lock (_lockObject)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[BLOCK {block.BlockNumber}] ⟳ Retrying ({retryCount})...");
                Console.ResetColor();
            }
        }

        public void ReportTransferStarted(long totalSize)
        {
            lock (_lockObject)
            {
                Console.WriteLine($"\n=== File Transfer Started ===");
                Console.WriteLine($"Total Size: {FormatBytes(totalSize)}");
                Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n");
            }
        }

        public void ReportTransferCompleted(long totalSize, TimeSpan duration)
        {
            lock (_lockObject)
            {
                var throughput = totalSize / duration.TotalSeconds;
                Console.WriteLine($"\n=== File Transfer Completed ===");
                Console.WriteLine($"Total Size: {FormatBytes(totalSize)}");
                Console.WriteLine($"Duration: {duration:hh\\:mm\\:ss\\.fff}");
                Console.WriteLine($"Throughput: {FormatBytes(throughput)}/s");
            }
        }

        public void ReportHashMismatch(int blockNumber, string sourceHash, string destHash)
        {
            lock (_lockObject)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[BLOCK {blockNumber}] Hash Mismatch:");
                Console.WriteLine($"  Source: {sourceHash}");
                Console.WriteLine($"  Destination:   {destHash}");
                Console.ResetColor();
            }
        }

        public void ReportFinalFileHashes(string sourceHash, string destHash, string algorithm)
        {
            lock (_lockObject)
            {
                Console.WriteLine($"\n=== File Hash Verification ({algorithm}) ===");
                Console.WriteLine($"Source File: {sourceHash}");
                Console.WriteLine($"Destination File:   {destHash}");

                if (sourceHash == destHash)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Hashes Match - Transfer Verified");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Hashes Mismatch - Transfer Failed");
                }
                Console.ResetColor();
            }
        }

        private string FormatBytes(double bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private static string ToHexString(byte[]? bytes)
        {
            if (bytes == null) return string.Empty;
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
