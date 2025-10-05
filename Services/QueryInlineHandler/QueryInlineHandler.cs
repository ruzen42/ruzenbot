using Microsoft.Extensions.Logging;
using RuzenBot.Services.Command;
using RuzenBot.Services.ShellRunner;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;

namespace RuzenBot.Services.QueryInlineHandler;

public class QueryInlineHandler(ITelegramBotClient botClient, IShellRunnerService shellRunnerService, ICommandService commandService, ILogger<QueryInlineHandler> logger) : IQueryInlineHandler 
{
    public async Task HandleInlineQuery(Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
    {
        var inlineQuery = update.InlineQuery!;
        var text = inlineQuery.Query;
        
        if (string.IsNullOrWhiteSpace(text)) 
        {
            logger.LogError("Inline query is null");
            return;
        }
        
        try
        {
            var rateOutput = RateInline(text);
            
            List<InlineQueryResult> results =
            [
                new InlineQueryResultArticle(
                    id: "2",
                    title: rateOutput, 
                    inputMessageContent: new InputTextMessageContent(rateOutput))
            ];
            logger.LogInformation("Inline query results: \n\t{ShellOutput}", rateOutput);

            await botClient.AnswerInlineQuery(inlineQuery.Id, results, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error while execute inline query: {ex}", ex.Message);
        }
    }

    private async Task<string> ShellRunnerInline(string query, CancellationToken cancellationToken)
    {
        var (output, error, exitCode) = await shellRunnerService.Execute(query, cancellationToken);
        return (exitCode == 0 ? output : output + error) ?? string.Empty; 
    }

    private string RateInline(string query)
    {
        query = query.ToLower();
        return $"{query} rate: {commandService.RateString(query)}/100";   
    }
}