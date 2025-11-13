using FileTransferTool.Application.DTOs;

namespace FileTransferTool.Application.Interfaces
{
    public interface IFileTransferUseCase
    {
        Task<FileTransferResult> ExecuteAsync(
            string sourceFile,
            string destinationPathOrFile,
            int threadCount = 2,
            CancellationToken cancellationToken = default);
    }
}
