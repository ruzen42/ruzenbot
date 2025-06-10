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

class RuzenBot
{
    private static readonly Logger logger = new(Logger.TypeLogger.Console);
    private static ITelegramBotClient? _botClient;
    private static ReceiverOptions? _receiverOptions;
    private static string? _token;

    private static class Commands
    {
        public const string Shell = "/shell";
        public const string Man = "/man";
        public const string Bash = "/bash";
        public const string Sh = "/sh";
        public const string Neofetch = "/neofetch";
    }

    static async Task Main(string[] args)
    {
        logger.CallStack = false;
        DotEnv.Load();
        var envVars = DotEnv.Read();
        _token = envVars["TOKEN"];
        if (string.IsNullOrEmpty(_token))
        {
            logger.Error("Bot token not found in environment variables. Please set the TOKEN variable.");
            Environment.Exit(1);
        }

        _botClient = new TelegramBotClient(_token);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        if (_botClient == null || _receiverOptions == null)
        {
            logger.Error("Bot client or receiver options not initialized.");
            Environment.Exit(1);
        }

        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
        var me = await _botClient.GetMeAsync();
        logger.Info($"Bot {me.FirstName} started");

        await Task.Delay(Timeout.Infinite, cts.Token);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                string messageText = update.Message.Text;
                long chatId = update.Message.Chat.Id;
                string? sendMessage = null;

                switch (messageText)
                {
                    case Commands.Neofetch:
                        sendMessage = await Shell("neofetch", "--stdout");
                        break;
                    case Commands.Shell:
                        sendMessage = "using: /shell [args]";
                        break;
                    case string s when s.StartsWith(Commands.Shell):
                        string cmd = s.Substring(Commands.Shell.Length).Trim();
                        if (!string.IsNullOrEmpty(cmd))
                        {
                            sendMessage = await Shell(cmd, "");
                        }
                        else
                        {
                            sendMessage = string.IsNullOrEmpty(cmd) ? "using: /shell [args]" : "Error: Command not allowed.";
                        }
                        break;
                    case Commands.Man:
                    case Commands.Bash:
                    case Commands.Sh:
                        sendMessage = "commands:\n\t/man - help\n\t/shell [command] - execute shell command\n\t/neofetch - system info";
                        break;
                    default:
                        logger.Warn($"Unknown command received: {messageText}");
                        break;
                }

                if (sendMessage == null || _botClient == null)
                {
                    logger.Warn("No message to send or bot client is null.");
                    return;
                }

                await _botClient.SendTextMessageAsync(chatId, sendMessage, cancellationToken: cancellationToken);
                logger.Info($"Message sent: {messageText}");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error: {ex}");
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n{apiRequestException.ErrorCode}",
            _ => error.ToString()
        };

        logger.Error($"Error: {errorMessage}");
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
            {
                logger.Error($"Shell command '{command} {arguments}' failed with exit code {process.ExitCode}: {error}");
                return $"Error executing command: {error}";
            }

            return output + error;
        }
        catch (Exception ex)
        {
            logger.Error($"Shell command execution failed: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }
}
