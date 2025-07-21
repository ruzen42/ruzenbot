using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class MessageHandler(ITelegramBotClient botClient, ICommandService commandService)
    : IMessageHandler
{
    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is null) return;

        var chatId = message.Chat.Id;
        var messageText = message.Text;
        var userId = message.From?.Id;

        logger.Info($"Received message from {userId}: {messageText}");

        if (messageText.StartsWith('/'))
        {
            var commandText = messageText.Split(' ')[0]; 
            
            if (await commandService.ExecuteCommandAsync(commandText, message, cancellationToken))
            {
                return; 
            }
            
            await botClient.SendMessage(
                chatId,
                "Неизвестная команда. Используйте /help для просмотра доступных команд.",
                cancellationToken: cancellationToken
            );
        }
    }
}