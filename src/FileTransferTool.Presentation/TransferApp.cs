using FileTransferTool.Application.Interfaces;
using FileTransferTool.Domain.Entities;

namespace FileTransferTool.Presentation
{
    /// <summary>
    /// Responsible for console interactions and invoking the application use case.
    /// </summary>
    public class TransferApp
    {
        private readonly IFileTransferUseCase _useCase;

        public TransferApp(IFileTransferUseCase useCase)
        {
            _useCase = useCase ?? throw new ArgumentNullException(nameof(useCase));
        }

        public async Task<int> RunAsync(string[]? args = null, CancellationToken cancellationToken = default)
        {
            try
            {
                int lastExitCode = 0;

                while (!cancellationToken.IsCancellationRequested) // outer loop: allow multiple transfers
                {
                    string sourceFile = string.Empty;
                    string destinationPath = string.Empty;

                    // Input/confirmation loop
                    while (true)
                    {
                        try
                        {
                            sourceFile = GetSourceFile();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Source input error: {ex.Message}");
                            Console.ResetColor();

                            if (!PromptRetryOrExit("Retry entering the source file? (R)etry / (E)xit: "))
                                return 0;
                            continue;
                        }

                        try
                        {
                            destinationPath = GetDestinationFile(sourceFile);
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Destination input error: {ex.Message}");
                            Console.ResetColor();

                            if (!PromptRetryOrExit("Retry entering the destination path? (R)etry / (E)xit: "))
                                return 0;
                            continue;
                        }

                        Console.WriteLine("\nYou entered:");
                        Console.WriteLine($"  Source:      {sourceFile}");
                        Console.WriteLine($"  Destination: {destinationPath}\n");

                        Console.Write("Proceed with these paths? (Y)es / (R)e-enter / (E)xit: ");
                        var choice = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;

                        if (string.IsNullOrEmpty(choice) || choice == "Y" || choice == "YES")
                            break; // proceed
                        if (choice == "E" || choice == "EXIT")
                            return 0; // exit app
                        // otherwise loop to re-enter
                    }

                    var threadCount = GetThreadCount();

                    // Attempt transfer. If it throws we let the user retry/exit/new paths.
                    var result = default(dynamic);
                    try
                    {
                        result = await _useCase.ExecuteAsync(
                            sourceFile,
                            destinationPath,
                            threadCount,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Transfer error: {ex.Message}");
                        Console.ResetColor();

                        Console.Write("Retry this transfer with the same paths? (R)etry / (N)ew paths / (E)xit: ");
                        var retryChoice = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;
                        if (retryChoice.StartsWith("R"))
                        {
                            Console.WriteLine("Retrying transfer...\n");
                            continue; // retry transfer with same paths
                        }
                        if (retryChoice.StartsWith("N"))
                        {
                            Console.WriteLine("Re-entering paths...\n");
                            continue; // go back to input loop to re-enter paths
                        }
                        return 1;
                    }

                    // Summary
                    Console.WriteLine("\n=== Transfer Summary ===");
                    Console.WriteLine($"Successful Blocks: {result.SuccessfulBlocks}/{result.BlockCount}");
                    Console.WriteLine($"Failed Blocks: {result.FailedBlocks}/{result.BlockCount}");
                    Console.WriteLine($"Overall Status: {(result.IsSuccessful ? "✓ SUCCESS" : "✗ FAILED")}");

                    // Block list & checksums (requirement #6)
                    Console.WriteLine("\n=== Block Checksums ===");
                    for (int i = 0; i < result.Blocks.Count; i++)
                    {
                        FileBlock b = result.Blocks[i];
                        var hex = b.SourceHash != null ? Convert.ToHexString(b.SourceHash).ToLower() : string.Empty;
                        Console.WriteLine($"{i + 1}. position = {b.Offset}, hash = {hex}");
                    }

                    lastExitCode = result.IsSuccessful ? 0 : 1;

                    // Ask whether to perform another transfer
                    Console.Write("\nDo another transfer? (Y)es / (N)o: ");
                    var again = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;
                    if (again.StartsWith("Y"))
                    {
                        Console.WriteLine();
                        continue; // repeat outer loop for a new transfer
                    }

                    return lastExitCode; // exit app with last transfer code
                }

                return lastExitCode;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static bool PromptRetryOrExit(string prompt)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;
            return input.StartsWith("R"); // 'R' to retry, anything else = exit
        }

        private static string GetSourceFile()
        {
            Console.Write("Enter source file path: ");
            var path = Console.ReadLine()?.Trim() ?? string.Empty;
            path = RemoveQuotes(path);

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Source file path cannot be empty");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Source file not found: {path}");
            return path;
        }

        private static string GetDestinationFile(string sourceFile)
        {
            Console.Write("Enter destination file path (file or directory): ");
            var input = Console.ReadLine()?.Trim() ?? string.Empty;
            input = RemoveQuotes(input);

            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Destination path cannot be empty");

            // If user provided an existing directory or a trailing separator -> treat as directory
            if (Directory.Exists(input) ||
                input.EndsWith(Path.DirectorySeparatorChar) ||
                input.EndsWith(Path.AltDirectorySeparatorChar))
            {
                Directory.CreateDirectory(input);
                return Path.Combine(input, Path.GetFileName(sourceFile));
            }

            // If input looks like a drive root ("C:") treat as directory
            if (input.Length == 2 && input[1] == Path.VolumeSeparatorChar)
            {
                input += Path.DirectorySeparatorChar;
                Directory.CreateDirectory(input);
                return Path.Combine(input, Path.GetFileName(sourceFile));
            }

            // Otherwise treat as file path; ensure parent exists
            var parent = Path.GetDirectoryName(input);
            if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            return input;
        }

        private static int GetThreadCount()
        {
            Console.Write("Enter number of transfer threads (default 2): ");
            var input = Console.ReadLine()?.Trim() ?? "2";
            if (int.TryParse(input, out var count) && count >= 1 && count <= 32)
                return count;
            return 2;
        }

        private static string RemoveQuotes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.StartsWith("\"") && input.EndsWith("\"") && input.Length >= 2)
                return input.Substring(1, input.Length - 2);

            if (input.StartsWith("'") && input.EndsWith("'") && input.Length >= 2)
                return input.Substring(1, input.Length - 2);

            return input;
        }
    }
}
