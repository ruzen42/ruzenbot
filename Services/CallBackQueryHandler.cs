using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class CallbackQueryHandler(ITelegramBotClient botClient, ILogger logger) : ICallbackQueryHandler
{
    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
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