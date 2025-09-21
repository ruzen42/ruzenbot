using Microsoft.Extensions.Logging;
using RuzenBot.Services.Bot;
using Telegram.Bot.Types;

namespace RuzenBot.Services.ConsoleCommand;

public class ConsoleService(ILogger<ConsoleService> logger, IBotService botService) : IConsoleService, IDisposable
{
    
    private readonly CancellationTokenSource _cts = new();
    private Task _consoleTask;
    private readonly List<ConsoleCommand> _commands = [];
    
    private record struct ConsoleCommand(string Name, string Description, Func<Task> Function);

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Console Service...");
        {
            _commands.Add(new ConsoleCommand("help", "Show help", ShowHelp));
            _commands.Add(new ConsoleCommand("exit", "Exit app", ExitImmediately));
            _commands.Add(new ConsoleCommand("clear", "clear", ClearConsole));
            _commands.Add(new ConsoleCommand("ping", "sent ping in telegram", PingBot));
        }
        _consoleTask = RunConsoleAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping Console Service...");
        await _cts.CancelAsync();
        
        if (_consoleTask != null) await _consoleTask;
    }

    private async Task RunConsoleAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.Write(">>> ");
                var input = await Task.Run(Console.ReadLine, cancellationToken);
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                
                await _commands.Find(x => x.Name.Equals(input.Trim(), StringComparison.CurrentCultureIgnoreCase)).Function();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError("Error processing console command: {ex}", ex);
            }
        }
    }


    private Task ShowHelp()
    {
        Console.WriteLine("Available commands:");
        _commands.ForEach(command => 
            Console.WriteLine(command.Name + " - " + command.Description));
        return Task.CompletedTask;
    }

    private Task ClearConsole()
    {
        Console.Clear();
        return Task.CompletedTask;
    }

    private Task ExitImmediately()
    {
        Environment.Exit(0);
        return Task.CompletedTask;
    }

    private async Task PingBot()
    {
        var message = new Telegram.Bot.Types.Message
        {
            Text = "Pong!",
            Chat = new Chat { Id = 1373776307 }
        };
        await botService.SendMessage(message);
    }

    public void Dispose() => _cts?.Dispose();
}