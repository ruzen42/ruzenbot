using Telegram.Bot;
using Telegram.Bot.Types;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, Command> _commands;
    private CancellationToken _cts;

    public CommandService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
        _commands = new Dictionary<string, Command>();
        RegisterDefaultCommands();
    }

    public async Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken)
    {
        _cts = cancellationToken;
        if (!_commands.TryGetValue(commandName, out var command)) return false; 
        try
        {
            await command.Handler(message);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Error executing command {commandName}: {ex}");
                
            await _botClient.SendMessage(
                message.Chat.Id,
                "Произошла ошибка при выполнении команды.",
                cancellationToken: cancellationToken
            );
                
            return true; 
        }
    }

    public void RegisterCommand(Command command)
    {
        _commands[command.Name] = command;
        logger.Info($"Command {command.Name} registered");
    }

    private void RegisterDefaultCommands()
    {
        RegisterCommand(new Command("/man", "Показать справку", HandleHelpCommand));
        RegisterCommand(new Command("/ping", "Проверить работу бота", HandlePingCommand));
        RegisterCommand(new Command("/sent", "Запустить программу", HandlerShell));
    }

    private async Task HandlerShell(Message message)
    {
        await _botClient.SendMessage(
            message.Chat.Id,
            "/sent",
            cancellationToken: _cts
        );
    }

    private async Task HandleStartCommand(Message message)
    {
        await _botClient.SendMessage(
            message.Chat.Id,
            "Добро пожаловать! Я ваш бот. Используйте /help для просмотра команд.",
            cancellationToken: _cts
        );
    }

    private async Task HandleHelpCommand(Message message)
    {
        var helpText = _commands.Values.Aggregate("Доступные команды:\n\n", (current, command) => current + (command.GetMan() + "\n"));

        await _botClient.SendMessage(
            message.Chat.Id,
            helpText, cancellationToken: _cts);
    }

    private async Task HandlePingCommand(Message message)
    {
        await _botClient.SendMessage(
            message.Chat.Id,
            "Pong! 🏓 Бот работает нормально.", cancellationToken: _cts);
    }
}