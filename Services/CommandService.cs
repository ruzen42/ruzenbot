using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, Command> _commands;
    private readonly CancellationToken _cts;

    public CommandService(ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        _cts = cancellationToken;
        _botClient = botClient;
        _commands = new Dictionary<string, Command>();
        RegisterDefaultCommands();
        _ = GetProcessOutput("TOKEN=null");
    }

    public async Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken)
    {
        if (!_commands.TryGetValue(commandName, out var command)) return false; 
        try
        {
            await command.Handler(message);
        }
        catch (Exception ex)
        {
            logger.Error($"Error executing command {commandName}: {ex}");
            await SendMessageWithReply("Ошибка при выполнении команды", message);
        }
        return true;
    }

    public void RegisterCommand(Command command)
    {
        _commands[command.Name] = command;
        logger.Info($"Command {command.Name} registered");
    }

    private void RegisterDefaultCommands()
    {
        RegisterCommand(new Command("/man", "Показать справку", HandleHelpCommand));
        RegisterCommand(new Command("/sent", "Запустить программу", HandlerShell));
        RegisterCommand(new Command("/id", "получить id", HandlerIdGet));
    }

    private async Task HandlerIdGet(Message message) => 
        await SendMessageWithReply((message.ReplyToMessage ?? message).From!.Id.ToString(), message);

    private async Task HandlerShell(Message message)
    {
        if (string.IsNullOrWhiteSpace(message.Text) || message.Text.Length <= 5)
        {
            await SendMessageWithReply($"Use {_commands["/sent"].Name} [args]", message);
            return;
        }

        var cmd = message.Text[5..].Trim();

        await SendMessageWithReply(GetProcessOutput(cmd).ToString(), message);
    }

    private async Task<string> GetProcessOutput(string cmd)
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
    
        await process.WaitForExitAsync(_cts).ConfigureAwait(true); 
        return await process.StandardOutput.ReadToEndAsync(_cts) + await process.StandardError.ReadToEndAsync(_cts);
    }

    private async Task HandleHelpCommand(Message message)
    {
        var helpText = _commands.Values.Aggregate("Доступные команды:\n\n", (current, command) => current + (command.GetMan() + "\n"));
        await SendMessageWithReply(helpText, message);
    }


    private async Task SendMessageWithReply(string sentMessage, Message messageToReply) => 
        await _botClient.SendMessage(
            messageToReply.Chat.Id,
            sentMessage,
            replyParameters: new ReplyParameters
            {
                MessageId = messageToReply.MessageId,  
                AllowSendingWithoutReply = true
            },
            cancellationToken: _cts); 
}