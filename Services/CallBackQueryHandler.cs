using Telegram.Bot;
using Telegram.Bot.Types;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class CallbackQueryHandler(ITelegramBotClient botClient) : ICallbackQueryHandler
{
    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data;

        logger.Info($"Received callback query: {data}");

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