using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RuzenBot.Services.Bot;

public class BotService(ITelegramBotClient botClient, IUpdateHandler updateHandler, ILogger<BotService> logger) : IBotService
{
    private const long YourId = 1373776307;
    private static readonly Chat YourChat = new() { Id = YourId };

    private static readonly Telegram.Bot.Types.Message StartMessage = new()
    {
        Chat = YourChat,
        Text = "Ruzenbot started"
    };
    
    private static readonly Telegram.Bot.Types.Message ExitMessage = new()
    {
        Chat = YourChat,
        Text = "Ruzenbot stoped"
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await botClient.DeleteWebhook(cancellationToken: cancellationToken);
            
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.ChatJoinRequest, UpdateType.InlineQuery],
                DropPendingUpdates = true 
            };

            botClient.StartReceiving(
                updateHandler.HandleUpdateAsync,
                updateHandler.HandleErrorAsync,
                receiverOptions,
                cancellationToken 
            );

            var me = await botClient.GetMe(cancellationToken);
            await SendMessage(StartMessage);
            logger.LogInformation("Bot @{MeUsername} started successfully", me.Username);
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error starting bot: {Exception}", ex);
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            await SendMessage(ExitMessage);
            logger.LogInformation("Bot stopped successfully");
        }
        catch (Exception ex)
        {
            logger.LogError("Error stopping bot: {Exception}", ex);
        }
    }

    public async Task SendMessage(Telegram.Bot.Types.Message message)
    {
        if (message.Text != null) await botClient.SendMessage(message.Chat.Id, message.Text);
    }
}
