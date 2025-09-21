using Microsoft.Extensions.Logging;
using RuzenBot.Services.ShellRunnerExecute;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;

namespace RuzenBot.Services.QueryInlineHandler;

public class QueryInlineHandler(ITelegramBotClient botClient, IShellRunnerService shellRunnerService, ILogger logger) : IQueryInlineHandler 
{
    
    public async Task HandleInlineQuery(Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
    {
        var inlineQuery = update.InlineQuery!;
        
        try
        {
            var results = new List<InlineQueryResult>
            {
                new InlineQueryResultArticle(
                    id: "1",
                    title: "Output",
                    inputMessageContent: new InputTextMessageContent((await shellRunnerService.Execute(inlineQuery.Query, cancellationToken)).ToString()!))
            };

            await botClient.AnswerInlineQuery(inlineQuery.Id, results);
        }
        catch (Exception ex)
        {
            logger.LogError($"Ошибка при обработке inline query: {ex.Message}");
        }
    }
}