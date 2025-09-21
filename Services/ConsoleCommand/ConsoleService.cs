using Microsoft.Extensions.Logging;

namespace RuzenBot.Services.ConsoleCommand;

public class ConsoleService(ILogger<ConsoleService> logger) : IConsoleService, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Task _consoleTask;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Console Service...");
        _consoleTask = RunConsoleAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping Console Service...");
        await _cts.CancelAsync();
        
        if (_consoleTask != null)
        {
            await _consoleTask;
        }
    }

    private async Task RunConsoleAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.Write("> ");
                var input = await Task.Run(Console.ReadLine, cancellationToken);
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                await ProcessCommandAsync(input.Trim(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing console command");
            }
        }
    }

    private Task ProcessCommandAsync(string command, CancellationToken cancellationToken)
    {
        switch (command.ToLower())
        {
            case "help":
                ShowHelp();
                break;
                
            case "exit":
            case "quit":
                logger.LogInformation("Shutting down application...");
                Environment.Exit(0);
                break;
        }

        return Task.CompletedTask;
    }

    private void ShowHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  help     - Show this help");
        Console.WriteLine("  status   - Show application status");
        Console.WriteLine("  users    - List recent users");
        Console.WriteLine("  user <id>- Get user info");
        Console.WriteLine("  exit     - Shutdown application");
        Console.WriteLine("  quit     - Shutdown application");
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}