using Telegram.Bot.Types;

namespace RuzenBot.Services.Command;

public interface ICommandService
{
    Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken);
}