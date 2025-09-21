using Microsoft.Extensions.Logging;
using RuzenBot.Services.ShellRunnerExecute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

public class InlineQueryHandler(ITelegramBotClient botClient, IShellRunnerService shellRunnerService, ILogger logger)
{
    private readonly CancellationTokenSource _cts = new();
    
    public async Task HandleInlineQuery(Update update)
    {
        var inlineQuery = update.InlineQuery!;
        
        try
        {
            var results = new List<InlineQueryResult>
            {
                new InlineQueryResultArticle(
                    id: "1",
                    title: "Output",
                    inputMessageContent: new InputTextMessageContent((await shellRunnerService.Execute(inlineQuery.Query, _cts.Token)).ToString()!))
            };


            await botClient.AnswerInlineQuery(inlineQuery.Id, results);
        }
        catch (Exception ex)
        {
            logger.LogError($"Ошибка при обработке inline query: {ex.Message}");
        }
    }
}