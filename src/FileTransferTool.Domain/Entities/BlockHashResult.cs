namespace FileTransferTool.Domain.Entities
{
    /// <summary>
    /// Result of a hash calculation for a block or full file.
    /// </summary>
    public class BlockHashResult
    {
        public int? BlockNumber { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public string HashHex => Convert.ToHexString(Hash).ToLower();
        public DateTime ComputedAt { get; set; }
        public string HashAlgorithm { get; set; } = string.Empty;
    }
}
