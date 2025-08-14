using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Net.Http;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using RuzenBot.Models;

namespace RuzenBot.Services;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, Command> _commands;
    private readonly ILogger _logger;
    private const string BaseUrl = "https://localhost:8080";
    
    private readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    
    private JsonSerializer _jsonSerializer = new();

    public CommandService(ITelegramBotClient botClient, ILogger logger)
    {
        _botClient = botClient;
        _logger = logger;
        _commands = new Dictionary<string, Command>();
        RegisterDefaultCommands();
        _logger.LogInformation(GetProcessOutput("unset TOKEN && unset API_OPEN_WEATHER && unset RCON_PASSWORD", CancellationToken.None).ToString());
    }

    public async Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken)
    {
        if (!_commands.TryGetValue(commandName, out var command)) return false; 
        try
        {
            await command.Handler(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error executing command {CommandName} (in context {message}: {Exception}", commandName, message.Text, ex);
            await SendMessageWithReply("Error with executing high level command: bot closed", message,  cancellationToken);
            Environment.Exit(1);
        }
        return true;
    }

    public void RegisterCommand(Command command) =>
        _commands[command.Name] = command;

    private void RegisterDefaultCommands()
    {
        RegisterCommand(new Command("/man", "Get help", HandleHelpCommand));
        RegisterCommand(new Command("/sent", "Start program", HandlerShell));
        RegisterCommand(new Command("/id", "Get ur id", HandlerIdGet));
    }

    private async Task HandlerIdGet(Message message, CancellationToken cancellationToken) => 
        await SendMessageWithReply((message.ReplyToMessage ?? message).From!.Id.ToString(), message, cancellationToken);

    private async Task HandlerShell(Message message, CancellationToken cancellationToken)
    {
        var request = new CommandRequest
        {
            Command = message.Text![..5].Trim(),
            ChatId = message.Chat.Id,
            UserId = message.From!.Id  
        };
        
        var response = new CommandResponse
        {
            Context = request,
            Error = $"Use /{_commands[2].Name} <arguments>",
            ExitCode = 0,
        };
        
        if (!(message.Text.Length <= 5))
            response = await GetProcessOutput(request, cancellationToken);
        
        await SendMessageWithReply(response.Output, message, cancellationToken);
    }
    
    private async Task HandleHelpCommand(Message message, CancellationToken cancellationToken) =>
        await SendMessageWithReply( 
            _commands.Values.Aggregate("Commands:\n\n", (current, command) 
                => current + (command.GetMan() + "\n")), message, cancellationToken);

    private async Task<CommandResponse> GetProcessOutput(CommandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _jsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/api/command/execute", content, cancellationToken);

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CommandResponse>(new JsonReader(responseJson));
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Error: {ex.Message}");
            return new CommandResponse { Error = ex.Message, Context = request, ExitCode = ex.HResult };
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Request timeout");
            return new CommandResponse { Error = "Request timeout", Context = request, ExitCode = 2};
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new CommandResponse { Error = ex.Message, Context = request, ExitCode = 1};
        }
    }

    private async Task SendMessageWithReply(string output, Message messageToReply, CancellationToken cancellationToken)
    {
        var replyParameters = new ReplyParameters
        {
            MessageId = messageToReply.MessageId,
            AllowSendingWithoutReply = true,
            QuoteParseMode = ParseMode.Markdown
        };

        await _botClient.SendMessage(messageToReply.Chat.Id, output, replyParameters: replyParameters, cancellationToken: cancellationToken);
    } 

}