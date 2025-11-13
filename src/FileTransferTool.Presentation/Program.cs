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
        // Composition root: register concrete implementations and the app runner
        var services = new ServiceCollection();

        // Infrastructure / presentation bindings
        services.AddSingleton<IBlockTransferService, BlockTransferService>();
        services.AddSingleton<IProgressReporter, ConsoleProgressReporter>();

        // IFileTransferUseCase requires two different IHashCalculator implementations,
        // so register it with a factory that constructs the calculators explicitly.
        services.AddSingleton<IFileTransferUseCase>(sp =>
            new FileTransferUseCase(
                sp.GetRequiredService<IBlockTransferService>(),
                new MD5HashCalculator(),    // per-block
                new SHA256HashCalculator(), // full-file
                sp.GetRequiredService<IProgressReporter>()));

        // App runner
        services.AddSingleton<TransferApp>();

        using var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<TransferApp>();

        return await app.RunAsync(args, CancellationToken.None);
    }
}