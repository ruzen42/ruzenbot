using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using RuzenBot.Models.ShellRunner;
using RuzenBot.Services.GithubApi;
using RuzenBot.Services.ShellRunnerExecute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RuzenBot.Services.Command;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, Models.Command> _commands;
    private readonly ILogger _logger;
    private readonly IShellRunnerService _shellRunnerService;
    private readonly IGithubApiService _githubApiService;
    
    public CommandService(ITelegramBotClient botClient, IShellRunnerService runner, ILogger logger, IGithubApiService githubApiService)
    {
        _botClient = botClient;
        _logger = logger;
        _shellRunnerService = runner;
        _githubApiService = githubApiService;
        _commands = new Dictionary<string, Models.Command>();
        RegisterDefaultCommands();
    }

    public async Task<bool> ExecuteCommandAsync(string commandName, Telegram.Bot.Types.Message message,
        CancellationToken cancellationToken)
    {
        if (!_commands.TryGetValue(commandName, out var command)) return false; 
        try
        {
            _logger.LogInformation($"Executing command: " +
                                   $"{commandName}\n\tChatId: " +
                                   $"{message.Chat.Id}\n\tMessage: " +
                                   $"{message.Text}\n\tUserId: {message.From!.Id}\n\tUsername: {message.From.Username}");
            await command.Handler(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error executing command {CommandName} (in context {message}: {Exception}", commandName, message.Text, ex);
            await SendMessageWithReply("Error", message,  cancellationToken);
        }
        return true;
    }

    private void RegisterCommand(Models.Command command) => _commands[command.Name] = command;

    private void RegisterDefaultCommands()
    {
        RegisterCommand(new Models.Command("/man", "Get help", HandleHelpCommand));
        RegisterCommand(new Models.Command("/rate", "Rate thing", HandlerRateCommand));
        RegisterCommand(new Models.Command("/ping", "Ping pong", HandlerPingCommand));
        RegisterCommand(new Models.Command("/sent", "Start program", HandlerShell));
        RegisterCommand(new Models.Command("/id", "Get ur id", HandlerIdGet));
        RegisterCommand(new Models.Command("/report", "Report message to admin", HandlerReport));
        RegisterCommand(new Models.Command("/gitrepo", "Get github repo (/gitrepo https://github.com/user/repo)", HandlerGithubRepoCommand));
        RegisterCommand(new Models.Command("/gituser", "Get github user (/gituser https://github.com/user)", HandlerGithubUserCommand));
    }
    private async Task HandlerPingCommand(Telegram.Bot.Types.Message message, CancellationToken cancellationToken) =>
        await SendMessageWithReply("Pong", message, cancellationToken);

    private async Task HandlerRateCommand(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        var text = message.Text![5..];
        var hasReply = message.ReplyToMessage != null;
        
        if (text.Length == 0 && !hasReply)
        {
            await SendMessageWithReply("Write a thing", message, cancellationToken);
            return;
        }

        if (hasReply)
            text = message.ReplyToMessage.Text!;
        
        var output = $"{text.ToLower()} rate: {RateString(text)}/100";
        
        await SendMessageWithReply(output, message, cancellationToken);
    }

    public int RateString(string text) => 
        text == null ? 50 : (text.GetHashCode() & 0x7FFFFFFF) % 101;

    private async Task HandlerGithubUserCommand(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        var url = message.Text![8..];
        if (string.IsNullOrWhiteSpace(url))
        {
            await SendMessageWithReply("Write a url", message, cancellationToken);
            return;
        }
        var result = await _githubApiService.GetUserData(url, cancellationToken);
        
        await SendMessageWithReply(result.ToString(), message, cancellationToken);
    }
    
    private async Task HandlerGithubRepoCommand(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        var url = message.Text![8..];
        if (string.IsNullOrWhiteSpace(url))
        {
            await SendMessageWithReply("Write a url", message, cancellationToken);
            return;
        }
        var result = await _githubApiService.GetRepoData(url, cancellationToken);
        
        await SendMessageWithReply(result.ToString(), message, cancellationToken);
    }

    private async Task HandlerIdGet(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        var result = (message.ReplyToMessage ?? message).From!.Id.ToString(); 
        
        await SendMessageWithReply(result, message, cancellationToken);
    }

    private async Task HandlerReport(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        if (message.ReplyToMessage == null)
        {
            await SendMessageWithReply("Reply to report", message, cancellationToken);
        }
        else
        {
            if (message.ReplyToMessage.From!.Id == message.From!.Id)
            {
                await SendMessageWithReply("You can't write a report or throw it in", message, cancellationToken);
                return;
            }
            
            var output = 
                         "Report summary\n@" + 
                         message.From!.Username + 
                         "\n" + message.Text![7..] + 
                         "\n" + message.ReplyToMessage!.Text + 
                         "\n@" + message.ReplyToMessage!.From!.Username;
            var adminChatId = new ChatId(1373776307);
            await _botClient.SendMessage(adminChatId, output, cancellationToken: cancellationToken);
            await SendMessageWithReply("Report successfully sent", message, cancellationToken);
        }
    }

    private async Task HandlerShell(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        var response = new CommandResponse
        {
            Output = "No Output",
            ExitCode = 0
        };
        
        if (!(message.Text!.Length <= _commands["/sent"].Name.Length))
            response = await _shellRunnerService.Execute(message.Text![5..].Trim(), cancellationToken); 
        
        await SendMessageWithReply(response.ToString(), message, cancellationToken);
    }

    private async Task HandleHelpCommand(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        var messageText = _commands.Values.Aggregate("Commands:\n\n", (current, command) => current + (command + "\n"));
        await SendMessageWithReply(messageText, message, cancellationToken);
    }

    private async Task SendMessageWithReply(string output, Telegram.Bot.Types.Message messageToReply, CancellationToken cancellationToken)
    {
        var replyParameters = new ReplyParameters
        {
            MessageId = messageToReply.MessageId,
            AllowSendingWithoutReply = true,
            QuoteParseMode = ParseMode.Markdown
        };

        await _botClient.SendMessage(messageToReply.Chat.Id, output, replyParameters: replyParameters, cancellationToken: cancellationToken);
    } 
}
