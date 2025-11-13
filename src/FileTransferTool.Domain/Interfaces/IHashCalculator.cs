namespace FileTransferTool.Domain.Interfaces
{
    /// <summary>
    /// Defines contract for hash calculation strategies.
    /// </summary>
    public interface IHashCalculator
    {
        string AlgorithmName { get; }

        /// <summary>
        /// Compute hash of a byte array.
        /// </summary>
        byte[] ComputeHash(byte[] data);

        /// <summary>
        /// Compute hash of a stream.
        /// </summary>
        byte[] ComputeHashStream(Stream stream);

        /// <summary>
        /// Compute hash of a file from specified offset and length.
        /// </summary>
        Task<byte[]> ComputeHashAsync(string filePath, long offset, long length);
    }
}
