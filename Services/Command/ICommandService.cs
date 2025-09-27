using Telegram.Bot.Types;

namespace RuzenBot.Services.Command;

public interface ICommandService
{
    Task<bool> ExecuteCommandAsync(string commandName, Telegram.Bot.Types.Message message,
        CancellationToken cancellationToken);

    int RateString(string text);
}