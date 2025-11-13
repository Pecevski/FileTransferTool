using FileTransferTool.Domain.Entities;

namespace FileTransferTool.Domain.Interfaces
{
    /// <summary>
    /// Defines contract for block transfer operations.
    /// </summary>
    public interface IBlockTransferService
    {
        /// <summary>
        /// Transfer a single block from source to destination with verification.
        /// </summary>
        Task<FileBlock> TransferBlockAsync(
            string sourceFile,
            string destinationFile,
            FileBlock block,
            IHashCalculator blockHashCalculator,
            CancellationToken cancellationToken);
    }
}
