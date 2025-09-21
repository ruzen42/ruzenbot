using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RuzenBot.Services.Bot;

public class BotService(ITelegramBotClient botClient, IUpdateHandler updateHandler, ILogger logger) : IBotService
{
    private CancellationTokenSource _cancellationTokenSource;
    private const long YourId = 1373776307;
    private static readonly Chat _yourChat = new() { Id = YourId };

    private static Telegram.Bot.Types.Message startMessage = new()
    {
        Chat = _yourChat,
        Text = "Ruzenbot started"
    };
    
    private static Telegram.Bot.Types.Message exitMessage = new()
    {
        Chat = _yourChat,
        Text = "Ruzenbot stoped"
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            await botClient.DeleteWebhook(cancellationToken: cancellationToken);
            
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
                DropPendingUpdates = true 
            };

            botClient.StartReceiving(
                updateHandler.HandleUpdateAsync,
                updateHandler.HandleErrorAsync,
                receiverOptions,
                _cancellationTokenSource.Token
            );

            var me = await botClient.GetMe(cancellationToken);
            await SendMessage(startMessage);
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
            await _cancellationTokenSource?.CancelAsync()!;
            await SendMessage(exitMessage);
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