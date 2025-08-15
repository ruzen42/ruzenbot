using Telegram.Bot;
using Telegram.Bot.Types;

namespace RuzenBot.Services.Update;

public interface IUpdateHandler
{
    Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken);
    Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken);
}