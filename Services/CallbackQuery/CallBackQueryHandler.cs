using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace RuzenBot.Services.CallbackQuery;

public class CallbackQueryHandler(ITelegramBotClient botClient, ILogger<CallbackQueryHandler> logger) : ICallbackQueryHandler
{
    public async Task HandleAsync(Telegram.Bot.Types.CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data;

        logger.LogInformation("Received callback query: {Data}", data);

        await botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            cancellationToken: cancellationToken
        );

        await botClient.SendMessage(
            chatId,
            $"Вы нажали кнопку: {data}",
            cancellationToken: cancellationToken
        );
    }
}