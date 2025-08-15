using RuzenBot.Services.Command;
using Telegram.Bot.Types;

namespace RuzenBot.Services;

public class MessageHandler(ICommandService commandService)
    : IMessageHandler
{
    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is null) return;

        var messageText = message.Text;

        if (messageText.StartsWith('/') || messageText.StartsWith('!'))
        {
            var commandText = messageText.Split(' ')[0];
            await commandService.ExecuteCommandAsync(commandText, message, cancellationToken);
        }
    }
}