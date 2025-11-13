using FileTransferTool.Domain.Entities;

namespace FileTransferTool.Application.Interfaces
{
    public interface IBlockCopyCoordinator
    {
        Task<List<FileBlock>> ProcessBlocksAsync(
            string sourceFile,
            string destinationFile,
            List<FileBlock> blocks,
            int maxConcurrency,
            CancellationToken cancellationToken = default);
    }
}
