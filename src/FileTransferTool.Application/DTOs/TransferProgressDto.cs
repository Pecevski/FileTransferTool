namespace FileTransferTool.Application.DTOs
{
    public class TransferProgressDto
    {
        public long TotalBytes { get; set; }
        public long TransferredBytes { get; set; }
        public int TotalBlocks { get; set; }
        public int CompletedBlocks { get; set; }
        public int FailedBlocks { get; set; }
        public double ProgressPercentage => TotalBytes > 0 ? (TransferredBytes * 100.0) / TotalBytes : 0;
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedRemainingTime { get; set; }
    }
}
