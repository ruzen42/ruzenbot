using NeoSimpleLogger;
using Telegram;
using Telegram.Bot;
using Telegram.Bot.Polling;
using dotenv.net;

namespace Ruzenbot;

class RuzenBot
{
    private Logger logger = new (Logger.TypeLogger.Console);

    private static ITelegramBotClient _botClient;
    private static ReceiverOptions _receiverOptions;
    private string _token;

    static async Task Main(string[] args)
    {
	DotEnv.Load();
	var envVars = DotEnv.Read();
	token = envVars["TOKEN"] ?? "";
	_botClient = new TelegramBotClient(token);
	_receiveOptions = new ReceiverOptions
	{
		AllowedUpdates = new[]
		{
			UpdateType.Message,
		},

		ThrowPendingUpdates = true,
	};

	using var cts = new CancellationTokenSource();

	_botClient.StartReceiving(UpdateHandler, ErrorHandle, _receiverOptions, cts.Token);
	var me = await _botClient.GetMeAsync();
	logger.Info($"Bot {me.FirstName} started");
	
	await Task.Delay(-1);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
	try
	{
		switch(update.Type)
		{
			case UpdateType.Message:
			{
				logger.Info("Message sent");
				return;
			}

		}
	}	
	catch (Exception ex)
	{
		logger.Error($"Error: {ex.ToString()}");
	}
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
    	var ErrorMessage = error switch
	{
		ApiRequestException apiRequestException 
			=> $"Telegram API Error:\n{apiRequestException.ErrorCode}",
		_ => error.ToString()
	};

	logger.Error($"Error: {ErrorMessage}");
	return Task.CompletedTask;
    }
}

