using Microsoft.Extensions.DependencyInjection;
using FileTransferTool.Application.Interfaces;
using FileTransferTool.Application.Services;
using FileTransferTool.Infrastructure.Hashing;
using FileTransferTool.Infrastructure.Threading;
using FileTransferTool.Presentation;
using FileTransferTool.Domain.Interfaces;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();

        // Infrastructure / presentation bindings
        services.AddSingleton<IBlockTransferService, BlockTransferService>();
        services.AddSingleton<IProgressReporter, ConsoleProgressReporter>();

        // IFileTransferUseCase requires two different IHashCalculator implementations: MD5 for per-block, SHA256 for full-file
        services.AddSingleton<IFileTransferUseCase>(sp =>
            new FileTransferUseCase(
                sp.GetRequiredService<IBlockTransferService>(),
                new MD5HashCalculator(),
                new SHA256HashCalculator(),
                sp.GetRequiredService<IProgressReporter>()));

        // App runner
        services.AddSingleton<TransferApp>();

        using var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<TransferApp>();

        return await app.RunAsync(args, CancellationToken.None);
    }
}