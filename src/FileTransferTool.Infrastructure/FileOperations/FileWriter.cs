using FileTransferTool.Infrastructure.Constants;

namespace FileTransferTool.Infrastructure.Services
{
    /// <summary>
    /// Handles writing file blocks to the destination file.
    /// </summary>
    public class FileWriter
    {
        public async Task WriteBlockAsync(
            string filePath,
            long offset,
            byte[] data,
            CancellationToken cancellationToken)
        {

            // Allow concurrent readers/writers; use async IO and consistent buffer size.
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite,
                IOConstants.FileStreamBufferSize,
                FileOptions.Asynchronous);

            stream.Seek(offset, SeekOrigin.Begin);
            await stream.WriteAsync(data, 0, data.Length, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
    }
}
