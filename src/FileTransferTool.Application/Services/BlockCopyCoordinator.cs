using FileTransferTool.Application.Interfaces;
using FileTransferTool.Domain.Entities;
using FileTransferTool.Domain.Enums;
using FileTransferTool.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Threading;

namespace FileTransferTool.Application.Services
{
    /// <summary>
    /// Coordinates parallel block transfer with retry logic and thread-safe synchronization.
    /// </summary>
    public class BlockCopyCoordinator  : IBlockCopyCoordinator
    {
        private readonly IBlockTransferService _blockTransferService;
        private readonly IHashCalculator _blockHashCalculator;
        private readonly IProgressReporter _progressReporter;
        private readonly int _maxRetries;
        private CancellationTokenSource? _sharedCts;

        public BlockCopyCoordinator(
            IBlockTransferService blockTransferService,
            IHashCalculator blockHashCalculator,
            IProgressReporter progressReporter,
            int maxRetries)
        {
            _blockTransferService = blockTransferService;
            _blockHashCalculator = blockHashCalculator;
            _progressReporter = progressReporter;
            _maxRetries = maxRetries;
        }

        /// <summary>
        /// Process blocks in parallel with controlled concurrency and retry logic.
        /// Cancels remaining work when any block permanently fails.
        /// </summary>
        public async Task<List<FileBlock>> ProcessBlocksAsync(
            string sourceFile,
            string destinationFile,
            List<FileBlock> blocks,
            int maxConcurrency,
            CancellationToken cancellationToken)
        {
            var results = new ConcurrentBag<FileBlock>();
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var failedBlocks = new ConcurrentBag<FileBlock>();

            _sharedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _sharedCts.Token;

            var tasks = blocks.Select(async block =>
            {
                try
                {
                    await semaphore.WaitAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // canceled prior to starting this block
                    block.Status = BlockTransferStatus.Failed;
                    block.ErrorMessage = "Cancelled";
                    failedBlocks.Add(block);
                    return;
                }

                try
                {
                    _progressReporter.ReportBlockStarted(block.BlockNumber, block.Size);
                    block.Status = BlockTransferStatus.InProgress;

                    var transferredBlock = await TransferBlockWithRetryAsync(
                        sourceFile,
                        destinationFile,
                        block,
                        token);

                    results.Add(transferredBlock);

                    if (transferredBlock.Status == BlockTransferStatus.Completed)
                    {
                        _progressReporter.ReportBlockCompleted(transferredBlock);
                    }
                    else
                    {
                        // Permanent failure - report and cancel remaining blocks
                        _progressReporter.ReportBlockFailed(transferredBlock, transferredBlock.ErrorMessage ?? "Unknown error");
                        failedBlocks.Add(transferredBlock);
                        try { _sharedCts.Cancel(); } catch { /* ignore */ }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // one of the blocks signalled cancel; swallow so we can return partial results
            }

            return results.ToList().OrderBy(b => b.BlockNumber).ToList();
        }

        private async Task<FileBlock> TransferBlockWithRetryAsync(
            string sourceFile,
            string destinationFile,
            FileBlock block,
            CancellationToken cancellationToken)
        {
            while (block.RetryCount <= _maxRetries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var transferredBlock = await _blockTransferService.TransferBlockAsync(
                        sourceFile,
                        destinationFile,
                        block,
                        _blockHashCalculator,
                        cancellationToken).ConfigureAwait(false);

                    if (transferredBlock.Status == BlockTransferStatus.Completed)
                        return transferredBlock;

                    if (transferredBlock.Status == BlockTransferStatus.VerificationFailed)
                    {
                        // report the two hashes for diagnostics
                        var srcHex = transferredBlock.SourceHash != null ? Convert.ToHexString(transferredBlock.SourceHash).ToLower() : string.Empty;
                        var dstHex = transferredBlock.DestinationHash != null ? Convert.ToHexString(transferredBlock.DestinationHash).ToLower() : string.Empty;
                        _progressReporter.ReportHashMismatch(transferredBlock.BlockNumber, srcHex, dstHex);

                        block.RetryCount++;
                        if (block.RetryCount <= _maxRetries)
                        {
                            block.Status = BlockTransferStatus.Retrying;
                            _progressReporter.ReportBlockRetry(block, block.RetryCount);
                            await Task.Delay(100 * block.RetryCount, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                    }

                    block.Status = BlockTransferStatus.Failed;
                    block.ErrorMessage = "Hash verification failed after max retries";
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
                    block.RetryCount++;
                    if (block.RetryCount <= _maxRetries)
                    {
                        block.Status = BlockTransferStatus.Retrying;
                        _progressReporter.ReportBlockRetry(block, block.RetryCount);
                        await Task.Delay(100 * block.RetryCount, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    block.Status = BlockTransferStatus.Failed;
                    block.ErrorMessage = ex.Message;
                    return block;
                }
            }

            block.Status = BlockTransferStatus.Failed;
            block.ErrorMessage = "Hash verification failed after max retries";
            return block;
        }
    }
}
