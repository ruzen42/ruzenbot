namespace RuzenBot.Services.Message;

public interface IMessageHandler
{
    Task HandleAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken);
}