using Microsoft.Extensions.Logging;
using RuzenBot.Services.CallbackQuery;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace RuzenBot.Services.Update;

public class UpdateHandler(IMessageHandler messageHandler, ILogger logger, ICallbackQueryHandler callbackQueryHandler)
    : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    await messageHandler.HandleAsync(update.Message!, cancellationToken);
                    break;
                case UpdateType.CallbackQuery:
                    await callbackQueryHandler.HandleAsync(update.CallbackQuery!, cancellationToken);
                    break;

                default:
                    logger.LogWarning("Unhandled update type: {UpdateType}", update.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error handling update: {Exception}", ex);
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

        logger.LogError("Bot error: {ErrorMessage}", errorMessage);

        if (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}