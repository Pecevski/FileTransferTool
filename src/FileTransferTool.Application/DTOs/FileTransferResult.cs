using FileTransferTool.Domain.Entities;

namespace FileTransferTool.Application.DTOs
{
    /// <summary>
    /// Output DTO representing the result of a file transfer operation.
    /// </summary>
    public class FileTransferResult
    {
        public string SourceFile { get; set; } = string.Empty;
        public string DestinationFile { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public int BlockCount { get; set; }
        public int SuccessfulBlocks { get; set; }
        public int FailedBlocks { get; set; }
        public byte[] SourceFileHash { get; set; } = Array.Empty<byte>();
        public byte[] DestinationFileHash { get; set; } = Array.Empty<byte>();
        public string HashAlgorithm { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public List<FileBlock> Blocks { get; set; } = new();

        public bool IsSuccessful => FailedBlocks == 0 && SourceFileHash.SequenceEqual(DestinationFileHash);
    }
}
