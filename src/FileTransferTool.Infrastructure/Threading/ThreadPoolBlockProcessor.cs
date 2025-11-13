using FileTransferTool.Domain.Entities;
using FileTransferTool.Domain.Enums;
using FileTransferTool.Domain.Interfaces;
using FileTransferTool.Infrastructure.Services;

namespace FileTransferTool.Infrastructure.Threading
{
    /// <summary>
    /// Implements the block transfer service with read-compute-write-verify workflow.
    /// </summary>
    public class BlockTransferService : IBlockTransferService
    {
        private readonly FileReader _fileReader;
        private readonly FileWriter _fileWriter;

        public BlockTransferService()
        {
            _fileReader = new FileReader();
            _fileWriter = new FileWriter();
        }

        public async Task<FileBlock> TransferBlockAsync(
            string sourceFile,
            string destinationFile,
            FileBlock block,
            IHashCalculator blockHashCalculator,
            CancellationToken cancellationToken)
        {
            try
            {
                // Read source block
                var sourceData = await _fileReader.ReadBlockAsync(
                    sourceFile,
                    block.Offset,
                    block.Size,
                    cancellationToken);

                // Compute source hash
                block.SourceHash = blockHashCalculator.ComputeHash(sourceData);

                // Write to destination (durable write)
                await _fileWriter.WriteBlockAsync(
                    destinationFile,
                    block.Offset,
                    sourceData,
                    cancellationToken);

                // Immediately read back from destination for verification
                var destData = await _fileReader.ReadBlockAsync(
                    destinationFile,
                    block.Offset,
                    block.Size,
                    cancellationToken);

                // Compute destination hash
                block.DestinationHash = blockHashCalculator.ComputeHash(destData);

                // Verify hashes match
                if (block.IsHashMatch)
                {
                    block.Status = BlockTransferStatus.Completed;
                }
                else
                {
                    block.Status = BlockTransferStatus.VerificationFailed;
                }

                return block;
            }
            catch (OperationCanceledException)
            {
                block.Status = BlockTransferStatus.Failed;
                block.ErrorMessage = "Cancelled";
                return block;
            }
            catch (Exception ex)
            {
                block.Status = BlockTransferStatus.Failed;
                block.ErrorMessage = ex.Message;
                return block;
            }
        }
    }
}
