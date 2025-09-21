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
        
        if (inlineQuery == null)
        {
            logger.LogError("Inline query is null");
            return;
        }
        
        try
        {
            var content = await shellRunnerService.Execute(inlineQuery.Query, cancellationToken);
            var results = new List<InlineQueryResult>
            {
                new InlineQueryResultArticle(
                    id: "1",
                    title: "Output: \n" + (string.IsNullOrWhiteSpace(content.Output) ? content.Output : content.Error), 
                    inputMessageContent: new InputTextMessageContent(content.ToString()!))
            };

            await botClient.AnswerInlineQuery(inlineQuery.Id, results, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error while execute inline query: {ex}", ex.Message);
        }
    }
}