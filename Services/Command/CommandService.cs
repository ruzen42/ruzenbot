using Microsoft.Extensions.Logging;
using RuzenBot.Models.ShellRunner;
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
    private readonly IShellRunnerHttp _shellRunnerHttp;
    
    public CommandService(ITelegramBotClient botClient, IShellRunnerHttp runner, ILogger logger)
    {
        _botClient = botClient;
        _logger = logger;
        _shellRunnerHttp = runner;
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
        RegisterCommand(new Models.Command("!ping", "Ping pong", HandlerPingCommand));
        RegisterCommand(new Models.Command("/sent", "Start program", HandlerShell));
        RegisterCommand(new Models.Command("/id", "Get ur id", HandlerIdGet));
    }
    private async Task HandlerPingCommand(Telegram.Bot.Types.Message message, CancellationToken cancellationToken) =>
        await SendMessageWithReply("Pong", message, cancellationToken);
    private async Task HandlerIdGet(Telegram.Bot.Types.Message message, CancellationToken cancellationToken) => 
        await SendMessageWithReply((message.ReplyToMessage ?? message).From!.Id.ToString(), message, cancellationToken);

    private async Task HandlerShell(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        var request = new CommandRequest
        {
            Command = message.Text![5..].Trim(),
            ChatId = message.Chat.Id,
            UserId = message.From!.Id  
        };
        
        var response = new CommandResponse
        {
            Output = "No Output",
            Context = request,
            ExitCode = 0
        };
        
        if (!(message.Text.Length <= _commands["/sent"].Name.Length))
            response = await _shellRunnerHttp.Execute(request, cancellationToken); 
        
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