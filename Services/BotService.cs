using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class BotService(ITelegramBotClient botClient, IUpdateHandler updateHandler, ILogger logger) : IBotService
{
    private CancellationTokenSource _cancellationTokenSource;

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
            logger.LogInformation("Bot @{MeUsername} started successfully", me.Username);
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error starting bot: {Exception}", ex);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _cancellationTokenSource?.CancelAsync()!;
            logger.LogInformation("Bot stopped successfully");
        }
        catch (Exception ex)
        {
            logger.LogError("Error stopping bot: {Exception}", ex);
        }
    }
}