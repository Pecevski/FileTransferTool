using System.Security.Cryptography;
using FileTransferTool.Domain.Interfaces;
using FileTransferTool.Infrastructure.Constants;

namespace FileTransferTool.Infrastructure.Hashing
{
    /// <summary>
    /// Implements full-file hash calculation using SHA256.
    /// </summary>
    public class SHA256HashCalculator : IHashCalculator
    {
        public string AlgorithmName => "SHA256";

        public byte[] ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(data);
        }

        public byte[] ComputeHashStream(Stream stream)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(stream);
        }

        public async Task<byte[]> ComputeHashAsync(string filePath, long offset, long length)
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                IOConstants.FileStreamBufferSize,
                FileOptions.Asynchronous);

            stream.Seek(offset, SeekOrigin.Begin);
            int bufferSize = (int)Math.Min(IOConstants.FileStreamBufferSize, length);
            var buffer = new byte[bufferSize];

            using var sha256 = SHA256.Create();
            long remaining = length;
            while (remaining > 0)
            {
                int bytesToRead = (int)Math.Min(buffer.Length, remaining);
                int bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead).ConfigureAwait(false);
                if (bytesRead == 0) break;
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                remaining -= bytesRead;
            }
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return sha256.Hash!;
        }
    }
}
