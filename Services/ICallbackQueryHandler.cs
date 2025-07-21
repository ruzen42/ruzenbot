using Telegram.Bot.Types;

namespace RuzenBot.Services;

public interface ICallbackQueryHandler
{
    Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken);
}