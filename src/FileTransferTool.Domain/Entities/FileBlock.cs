using FileTransferTool.Domain.Enums;

namespace FileTransferTool.Domain.Entities
{
    /// <summary>
    /// Represents a logical block of a file to be transferred.
    /// </summary>
    public class FileBlock
    {
        public int BlockNumber { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public byte[]? SourceHash { get; set; }
        public byte[]? DestinationHash { get; set; }
        public int RetryCount { get; set; }
        public BlockTransferStatus Status { get; set; }
        public string? ErrorMessage { get; set; }

        public bool IsHashMatch => SourceHash != null && DestinationHash != null &&
                                   SourceHash.SequenceEqual(DestinationHash);
    }
}
