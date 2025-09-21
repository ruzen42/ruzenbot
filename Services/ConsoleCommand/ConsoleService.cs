using Microsoft.Extensions.Logging;
using RuzenBot.Services.Bot;
using RuzenBot.Services.GithubApi;
using RuzenBot.Services.ShellRunnerExecute;
using Telegram.Bot.Types;

namespace RuzenBot.Services.ConsoleCommand;

public class ConsoleService(
    ILogger<ConsoleService> logger, 
    IBotService botService, 
    IGithubApiService githubApiService,
    IShellRunnerService shellRunnerService) 
    : IConsoleService, IDisposable
{
    
    private readonly CancellationTokenSource _cts = new();
    private readonly string _username = Environment.UserName;
    private readonly List<ConsoleCommand> _commands = [];
    
    private record struct ConsoleCommand(string Name, string Description, Func<Task> Function);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Console Service...");
        {
            _commands.Add(new ConsoleCommand("help", "Show help", ShowHelp));
            _commands.Add(new ConsoleCommand("exit", "Exit app", ExitImmediately));
            _commands.Add(new ConsoleCommand("clear", "clear", ClearConsole));
            _commands.Add(new ConsoleCommand("ping", "sent ping in telegram", BotPing));
            _commands.Add(new ConsoleCommand("gitUserPing", "ping test user", GitHubApiUserPing));
            _commands.Add(new ConsoleCommand("gitRepoPing", "ping test repo", GitHubApiRepoPing));
            _commands.Add(new ConsoleCommand("shellPing", "uname ping", ShellRunnerPing));
        }
        await RunConsoleAsync(_cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default) =>
        await _cts.CancelAsync();

    private async Task RunConsoleAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.Write(_username + " -> ");
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

    private async Task BotPing()
    {
        var message = new Telegram.Bot.Types.Message
        {
            Text = "Pong!",
            Chat = new Chat { Id = 5727604888 }
        };
        await botService.SendMessage(message);
    }

    private async Task ShellRunnerPing()
    {
        try
        {
            var content = await shellRunnerService.Execute("uname", _cts.Token);
            Console.WriteLine(content.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError("Error with pinging shellrunner: {ex}", ex.Message);
        }
    }

    private async Task GitHubApiUserPing()
    {
        try
        {
            var content = await githubApiService.GetUserData("https://github.com/ruzen42", _cts.Token);
            Console.WriteLine(content.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError("Error ping GitHub User API: {ex}", ex.Message);
        }
    }
    private async Task GitHubApiRepoPing()
    {
        try
        {
            var content = await githubApiService.GetUserData("https://github.com/ruzen42", _cts.Token);
            Console.WriteLine(content.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError("Error ping GitHub Repo API: {ex}", ex.Message);
        }
    }

    public void Dispose() => _cts?.Dispose();
}