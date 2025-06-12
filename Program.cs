using NeoSimpleLogger;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using dotenv.net;

namespace Ruzenbot;

internal abstract class RuzenBot
{
    private static readonly Logger Logger = new(Logger.TypeLogger.Console);
    private static ITelegramBotClient? _botClient;
    private static ReceiverOptions? _receiverOptions;
    private static string? _token;
    private static int? _maxChars = 4096;
    private static readonly HashSet<long> _rootsUsers = [1373776307];
    private static HashSet<long> _rootsGroups = [-1002422734147];
    private static bool _stopping = false;

    private static class Commands
    {
        public const string Man = "/man";
        public const string SentCommand = "/sent";
        public const string IdGet = "/id";
        public const string Neofetch = "/neofetch";
        public const string Docker = "/docker";
    }

    private static async Task Main(string[] args)
    {
        
        if (args.Length > 1)
        {
            _maxChars = Convert.ToInt32(args[1]);
        }
        
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
        }
        Logger.Warn($"TOKEN={_token?[1..10]}");

        if (string.IsNullOrEmpty(_token))
        {
            Logger.Error("Bot token not found in environment variables. Please set the TOKEN variable.");
            Environment.Exit(1);
        }

        _botClient = new TelegramBotClient(_token);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
        var me = await _botClient.GetMeAsync(cancellationToken: cts.Token);
        Logger.Info($"Bot {me.FirstName} started with max chars: {_maxChars}");

        await Task.Delay(Timeout.Infinite, cts.Token);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update is { Type: UpdateType.Message, Message.Text: not null })
            {
                var messageText = update.Message.Text;
                var chatId = update.Message.Chat.Id;
                string? sendMessage = null;
		        var user = update.Message.From;
                _stopping = false;
                if (!_rootsGroups.Contains(chatId)) return;

                string cmd;
                switch (messageText)
                {
                    case not null when messageText.StartsWith(Commands.Neofetch):
                        sendMessage = await Shell("neofetch", "--stdout");
                        break;
                    case Commands.SentCommand:
                        sendMessage = $"using: {Commands.SentCommand} [args]";
                        break;
                    case not null when messageText.StartsWith(Commands.IdGet):
                        sendMessage = update.Message.ReplyToMessage?.From?.Id.ToString();
			            break;
                    case not null when messageText.StartsWith(Commands.Docker):
                        cmd = messageText[Commands.Docker.Length..].Trim();

                        switch (cmd)
                        {
                            case "stop":
                            {
                                if (!CheckRoot()) break;
                                Logger.Info($"Bot stopping by command @{user?.Username ?? "anon"}");
                                _stopping = true;
                                sendMessage = "Bot stopped";
                                break;
                            }

                            case "stat":
                            {
                                sendMessage = $"Stats:\n\tMax chars per message: {_maxChars}";
                                break;
                            }

                            default:
                            {
                                sendMessage = $"Not command entered {cmd}";
                                break;
                            }
                        }
                        break;

                        bool CheckRoot()
                        {
                            if (_rootsUsers.Contains((long)user?.Id!)) return true;
                            sendMessage = "Permission denied. This accident will sent to admin";
                            Logger.Warn($"{user?.Id} {user?.Username} Permission denied ");
                            return false;
                        }
                    case not null when messageText.StartsWith(Commands.SentCommand):
                        cmd = messageText[Commands.SentCommand.Length..].Trim();
                        if (!string.IsNullOrEmpty(cmd))
                        {
                            sendMessage = await Shell(cmd, "");
                            if (string.IsNullOrEmpty(sendMessage)) sendMessage = "No output";
                            if (sendMessage.Length > (_maxChars ?? 100)) sendMessage = "So big output";
                        }
                        else
                        {
                            sendMessage = "Command is empty";
                        }
                        break;
                    case not null when messageText.StartsWith(Commands.Man):
                        sendMessage = $"Commands:" +
                                      $"\n\t{Commands.Man} - help" +
                                      $"\n\t{Commands.SentCommand} [command] - execute shell command" +
                                      $"\n\t{Commands.Neofetch} - system info" +
                                      $"\n\t{Commands.Docker} - docker container administration [run,stat]";
                        break;
                    default:
                        return;
                }
                Logger.Info($"Message sent: {messageText}\nwhere: {chatId}\nfrom: {user?.Username ?? "user" }\t{user!.Id}");


                if (_botClient == null || sendMessage == null)
                {
                    Logger.Warn("No message to send or bot client is null.");
                    return;
                }

                await _botClient.SendTextMessageAsync(chatId, sendMessage, replyToMessageId: update.Message.MessageId, cancellationToken: cancellationToken);
                if (_stopping) Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex.ToString()[10..]}");
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var errorMessage = error switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error {apiRequestException.ErrorCode}",
            _ => error.ToString()
        };

        Logger.Error($"Error: {errorMessage}");
        return Task.CompletedTask;
    }

    private static async Task<string> Shell(string command, string arguments)
    {
        if (command.StartsWith(":(){ :|:& };:")
            || command.StartsWith("fork")) return "fork bomb detected"; 
        
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = $"-c \"{command} {arguments}\""
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                Logger.Error($"Shell command '{command} {arguments}' failed with exit code {process.ExitCode}: {error}");
            
            var cancellationTokenSource = new CancellationTokenSource(5000);

            try
            {
                await process.WaitForExitAsync(cancellationTokenSource.Token);
                return output + error;
            }
            catch (TaskCanceledException)
            {
                process.Kill();
                Logger.Warn("So slow");
                return "So slow";
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Shell command execution failed: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }
}
