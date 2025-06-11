using NeoSimpleLogger;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using dotenv.net;

namespace Ruzenbot;

internal class RuzenBot
{
    private static readonly Logger Logger = new(Logger.TypeLogger.Console);
    private static ITelegramBotClient? _botClient;
    private static ReceiverOptions? _receiverOptions;
    private static string? _token;

    private static class Commands
    {
        public const string Man = "/man";
        public const string Shell = "/shell";
        public const string MyId = "/myid";
        public const string Bash = "/bash";
        public const string Sh = "/sh";
        public const string Neofetch = "/neofetch";

    }

    static async Task Main(string[] args)
    {
        Logger.CallStack = false;
        DotEnv.Load();
        var envVars = DotEnv.Read();
	    try
	    {
        	_token = envVars["TOKEN"];
        }
        catch (Exception)
        {
            _token = Environment.GetEnvironmentVariable("TOKEN");
            Logger.Info("Using TOKEN VAR");
        }
        Logger.Warn($"TOKEN={_token?[1..10]}");

        if (string.IsNullOrEmpty(_token))
        {
            Logger.Error("Bot token not found in environment variables. Please set the TOKEN variable.");
            Environment.Exit(1);
        }
        Logger.Info(".env file deleted" + Shell("rm .env", ""));

        _botClient = new TelegramBotClient(_token);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
        var me = await _botClient.GetMeAsync(cancellationToken: cts.Token);
        Logger.Info($"Bot {me.FirstName} started");

        await Task.Delay(Timeout.Infinite, cts.Token);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update is { Type: UpdateType.Message, Message.Text: not null })
            {
                string messageText = update.Message.Text;
                long chatId = update.Message.Chat.Id;
                string? sendMessage = null;
		        var user = update.Message.From;
                Logger.Info($"Message sent: {messageText}\nwhere: {chatId}\nfrom: {user?.Username ?? "user" }\t{user!.Id}");

                switch (messageText)
                {
                    case Commands.Neofetch:
                        sendMessage = await Shell("neofetch", "--stdout");
                        break;
                    case Commands.Shell:
                        sendMessage = $"using: {Commands.Shell} [args]";
                        break;
                    case { } s when s.StartsWith(Commands.MyId):
			            sendMessage = user.Id.ToString() ?? "1488";
			            break;
                    case { } s when s.StartsWith(Commands.Shell):
                        string cmd = s[Commands.Shell.Length..].Trim();
                        if (!string.IsNullOrEmpty(cmd))
                        {
                            sendMessage = await Shell(cmd, "");
                            if (string.IsNullOrEmpty(sendMessage)) sendMessage = "no output";
                            if (sendMessage.Length > 4095) sendMessage = "so big output";
                        }
                        else
                        {
                            sendMessage = "Command is empty";
                        }
                        break;
                    case Commands.Man:
                    case Commands.Bash:
                    case Commands.Sh:
                        sendMessage = $"commands:\n\t/man - help\n\t{Commands.Shell} [command] - execute shell command\n\t/neofetch - system info";
                        break;
                }


                if (sendMessage == null || _botClient == null)
                {
                    Logger.Warn("No message to send or bot client is null.");
                    return;
                }

                await _botClient.SendTextMessageAsync(chatId, sendMessage, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex.ToString().Substring(10)}");
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n{apiRequestException.ErrorCode}",
            _ => error.ToString()
        };

        Logger.Error($"Error: {errorMessage}");
        return Task.CompletedTask;
    }

    private static async Task<string> Shell(string command, string arguments)
    {
        try
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = $"-c \"{command} {arguments}\""
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                Logger.Error($"Shell command '{command} {arguments}' failed with exit code {process.ExitCode}: {error}");

            return output + error;
        }
        catch (Exception ex)
        {
            Logger.Error($"Shell command execution failed: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }
}


