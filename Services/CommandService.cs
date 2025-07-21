using Telegram.Bot;
using Telegram.Bot.Types;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, Command> _commands;

    public CommandService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
        _commands = new Dictionary<string, Command>();
        RegisterDefaultCommands();
    }

    public async Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken)
    {
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
        RegisterCommand(new Command("/start", "Запустить бота", HandleStartCommand));
        RegisterCommand(new Command("/help", "Показать справку", HandleHelpCommand));
        RegisterCommand(new Command("/ping", "Проверить работу бота", HandlePingCommand));
    }

    private async Task HandleStartCommand(Message message)
    {
        await _botClient.SendMessage(
            message.Chat.Id,
            "Добро пожаловать! Я ваш бот. Используйте /help для просмотра команд."
        );
    }

    private async Task HandleHelpCommand(Message message)
    {
        var helpText = _commands.Values.Aggregate("Доступные команды:\n\n", (current, command) => current + (command.GetMan() + "\n"));

        await _botClient.SendMessage(
            message.Chat.Id,
            helpText
        );
    }

    private async Task HandlePingCommand(Message message)
    {
        await _botClient.SendMessage(
            message.Chat.Id,
            "Pong! 🏓 Бот работает нормально."
        );
    }
}