namespace RuzenBot.Services.QueryInlineHandler;

public interface IQueryInlineHandler
{
    Task HandleInlineQuery(Telegram.Bot.Types.Update update, CancellationToken cancellationToken);
}