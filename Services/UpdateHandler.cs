using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class UpdateHandler(IMessageHandler messageHandler, ICallbackQueryHandler callbackQueryHandler)
    : IUpdateHandler
{
    private readonly IMessageHandler _messageHandler = messageHandler;
    private readonly ICallbackQueryHandler _callbackQueryHandler = callbackQueryHandler;

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    await _messageHandler.HandleAsync(update.Message!, cancellationToken);
                    break;
                case UpdateType.CallbackQuery:
                    await _callbackQueryHandler.HandleAsync(update.CallbackQuery!, cancellationToken);
                    break;
                default:
                    logger.Warn($"Unhandled update type: {update.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error handling update: {ex}");
        }
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.Error($"Bot error: {errorMessage}");

        // Задержка перед повторной попыткой
        if (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}