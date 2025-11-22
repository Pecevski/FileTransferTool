using FileTransferTool.Infrastructure.Constants;

namespace FileTransferTool.Infrastructure.Services
{
    /// <summary>
    /// Handles reading file blocks from the source file.
    /// </summary>
    public class FileReader
    {
        public async Task<byte[]> ReadBlockAsync(
            string filePath,
            long offset,
            long length,
            CancellationToken cancellationToken)
        {
            // Allow read while other handles are writing (FileShare.ReadWrite).
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                IOConstants.FileStreamBufferSize,
                FileOptions.Asynchronous);

            stream.Seek(offset, SeekOrigin.Begin);
            var buffer = new byte[(int)length];
            await stream.ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken);
            return buffer;
        }
    }
}
