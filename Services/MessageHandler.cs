using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class MessageHandler(ITelegramBotClient botClient, ICommandService commandService, ILogger logger)
    : IMessageHandler
{
    public ITelegramBotClient BotClient { get; } = botClient;

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is null) return;

        var messageText = message.Text;

        if (messageText.StartsWith('/'))
        {
            var commandText = messageText.Split(' ')[0];

            await commandService.ExecuteCommandAsync(commandText, message, cancellationToken);
            logger.LogInformation("Command received: {Command}", commandText);
        }
    }
}