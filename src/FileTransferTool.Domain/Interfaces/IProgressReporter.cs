using FileTransferTool.Domain.Entities;

namespace FileTransferTool.Domain.Interfaces
{
    /// <summary>
    /// Defines contract for progress reporting.
    /// </summary>
    public interface IProgressReporter
    {
        void ReportBlockStarted(int blockNumber, long size);
        void ReportBlockCompleted(FileBlock block);
        void ReportBlockFailed(FileBlock block, string error);
        void ReportBlockRetry(FileBlock block, int retryCount);
        void ReportTransferStarted(long totalSize);
        void ReportTransferCompleted(long totalSize, TimeSpan duration);
        void ReportHashMismatch(int blockNumber, string sourceHash, string destHash);
        void ReportFinalFileHashes(string sourceHash, string destHash, string algorithm);
    }
}
