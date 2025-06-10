using NeoSimpleLogger;
using System.IO;
using System.Diagnostics;
using Telegram;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using dotenv.net;

namespace Ruzenbot;

class RuzenBot
{
    private static Logger logger = new (Logger.TypeLogger.Console);

    private static ITelegramBotClient? _botClient;
    private static ReceiverOptions? _receiverOptions;
    private static string? _token;

    static async Task Main(string[] args)
    {
	logger.CallStack = false;
	DotEnv.Load();
	var envVars = DotEnv.Read();
	_token = envVars["TOKEN"] ?? "real porn";
	_botClient = new TelegramBotClient(_token);
	_receiverOptions = new ReceiverOptions
	{
		AllowedUpdates = new[]
		{
			UpdateType.Message,
		},
	};

	using var cts = new CancellationTokenSource();

	_botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
	var me = await _botClient.GetMeAsync();
	logger.Info($"Bot {me.FirstName} started");
	
	await Task.Delay(-1);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
	try
	{
		if (update.Type == UpdateType.Message && update.Message?.Text != null)
		{	
			string messageText = update.Message.Text;
			long chatId = update.Message.Chat.Id;
			string? sendMessage = "null";

			switch (messageText)
			{
				case "/neofetch":
					sendMessage = Shell("neofetch");
					break;
			}

			if (sendMessage == "null" && _botClient == null) return;

			await _botClient.SendTextMessageAsync(chatId, sendMessage);

			logger.Info($"Message sent: {messageText}");
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

    private static string Shell(string command)	
    {
	    Process process = new Process
	    {
		    StartInfo = new ProcessStartInfo
	            {
			    FileName = command,
			    UseShellExecute = false,
			    RedirectStandardOutput = true,
			    CreateNoWindow = true,
			    Arguments = "--stdout"
		    }
	    };
	    
	    string output = "1488";

	    process.Start();
	    StreamReader reader = process.StandardOutput;
	    output = reader.ReadToEnd();
	    return output;
    }
}

