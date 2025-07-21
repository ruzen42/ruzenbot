using Telegram.Bot.Types;

namespace RuzenBot.Services;

public interface ICommandService
{
    Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken);
    void RegisterCommand(Command command);
}