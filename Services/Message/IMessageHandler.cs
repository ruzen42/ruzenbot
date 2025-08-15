using Telegram.Bot.Types;

namespace RuzenBot.Services;

public interface IMessageHandler
{
    Task HandleAsync(Message message, CancellationToken cancellationToken);
}