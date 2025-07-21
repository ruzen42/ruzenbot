using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using static RuzenBot.Program;

namespace RuzenBot.Services;

public class BotService(ITelegramBotClient botClient, IUpdateHandler updateHandler) : IBotService
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
            };

            botClient.StartReceiving(
                updateHandler.HandleUpdateAsync,
                updateHandler.HandleErrorAsync,
                receiverOptions,
                _cancellationTokenSource.Token
            );

            var me = await botClient.GetMe(cancellationToken);
            logger.Info($"Bot @{me.Username} started successfully");
        }
        catch (Exception ex)
        {
            logger.Fatal($"Error starting bot: {ex}");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _cancellationTokenSource?.CancelAsync();
            logger.Info("Bot stopped successfully");
        }
        catch (Exception ex)
        {
            logger.Error($"Error stopping bot: {ex}");
        }
    }
}