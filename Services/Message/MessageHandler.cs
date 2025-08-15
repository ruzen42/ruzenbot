using RuzenBot.Services.Command;

namespace RuzenBot.Services.Message;

public class MessageHandler(ICommandService commandService)
    : IMessageHandler
{
    public async Task HandleAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
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