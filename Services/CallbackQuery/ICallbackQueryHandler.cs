namespace RuzenBot.Services.CallbackQuery;

public interface ICallbackQueryHandler
{
    Task HandleAsync(Telegram.Bot.Types.CallbackQuery callbackQuery, CancellationToken cancellationToken);
}