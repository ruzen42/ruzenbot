using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RuzenBot.Services;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, Command> _commands;
    private readonly ILogger _logger;

    public CommandService(ITelegramBotClient botClient, ILogger logger)
    {
        _botClient = botClient;
        _logger = logger;
        _commands = new Dictionary<string, Command>();
        RegisterDefaultCommands();
        _ = GetProcessOutput("TOKEN=null", CancellationToken.None);
    }

    public async Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken)
    {
        if (!_commands.TryGetValue(commandName, out var command)) return false; 
        try
        {
            await command.Handler(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error executing command {CommandName}: {Exception}", commandName, ex);
            await SendMessageWithReply("Ошибка при выполнении команды", message,  cancellationToken);
        }
        return true;
    }

    public void RegisterCommand(Command command)
    {
        _commands[command.Name] = command;
        _logger.LogInformation("Command {CommandName} registered", command.Name);
    }

    private void RegisterDefaultCommands()
    {
        RegisterCommand(new Command("/man", "Показать справку", HandleHelpCommand));
        RegisterCommand(new Command("/sent", "Запустить программу", HandlerShell));
        RegisterCommand(new Command("/id", "получить id", HandlerIdGet));
    }

    private async Task HandlerIdGet(Message message, CancellationToken cancellationToken) => 
        await SendMessageWithReply((message.ReplyToMessage ?? message).From!.Id.ToString(), message, cancellationToken);

    private async Task HandlerShell(Message message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Text) || message.Text.Length <= 5)
        {
            await SendMessageWithReply($"Use {_commands["/sent"].Name} [args]", message, cancellationToken);
            return;
        }

        var cmd = message.Text[5..].Trim();
        await SendMessageWithReply(await GetProcessOutput(cmd, cancellationToken), message, cancellationToken);
    }

    private async Task<string> GetProcessOutput(string cmd, CancellationToken cancellationToken)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = "-c \"" + cmd.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;

        process.Start();
    
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(true); 
        return await process.StandardOutput.ReadToEndAsync(cancellationToken) + await process.StandardError.ReadToEndAsync(cancellationToken);
    }

    private async Task HandleHelpCommand(Message message, CancellationToken cancellationToken)
    {
        var helpText = _commands.Values.Aggregate("Доступные команды:\n\n", (current, command) => current + (command.GetMan() + "\n"));
        await SendMessageWithReply(helpText, message, cancellationToken);
    }


    private async Task SendMessageWithReply(string sentMessage, Message messageToReply, CancellationToken cancellationToken) => 
        await _botClient.SendMessage(
            messageToReply.Chat.Id,
            sentMessage,
            replyParameters: new ReplyParameters
            {
                MessageId = messageToReply.MessageId,  
                AllowSendingWithoutReply = true
            },
            cancellationToken: cancellationToken);
}