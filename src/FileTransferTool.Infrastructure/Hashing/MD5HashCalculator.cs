using System.Security.Cryptography;
using FileTransferTool.Domain.Interfaces;
using FileTransferTool.Infrastructure.Constants;

namespace FileTransferTool.Infrastructure.Hashing
{
    /// <summary>
    /// Implements block-level hash calculation using MD5.
    /// </summary>
    public class MD5HashCalculator : IHashCalculator
    {
        public string AlgorithmName => "MD5";

        public byte[] ComputeHash(byte[] data)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(data);
        }

        public byte[] ComputeHashStream(Stream stream)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(stream);
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

            using var md5 = MD5.Create();
            long remaining = length;
            while (remaining > 0)
            {
                int bytesToRead = (int)Math.Min(buffer.Length, remaining);
                int bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead).ConfigureAwait(false);
                if (bytesRead == 0) break;
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                remaining -= bytesRead;
            }
            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return md5.Hash!;
        }
    }
}
