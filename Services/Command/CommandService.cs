using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RuzenBot.Models.ShellRunner;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RuzenBot.Services.Command;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, RuzenBot.Command> _commands;
    private readonly ILogger _logger;
    private const string BaseUrl = "http://localhost:8080";
    
    private readonly HttpClient _httpClient = new() 
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    
    public CommandService(ITelegramBotClient botClient, ILogger logger)
    {
        _botClient = botClient;
        _logger = logger;
        _commands = new Dictionary<string, RuzenBot.Command>();
        RegisterDefaultCommands();
    }

    public async Task<bool> ExecuteCommandAsync(string commandName, Message message, CancellationToken cancellationToken)
    {
        if (!_commands.TryGetValue(commandName, out var command)) return false; 
        try
        {
            await command.Handler(message, cancellationToken);
            _logger.LogInformation("Command received: {Command}", commandName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error executing command {CommandName} (in context {message}: {Exception}", commandName, message.Text, ex);
            await SendMessageWithReply("Error", message,  cancellationToken);
        }
        return true;
    }

    public void RegisterCommand(RuzenBot.Command command) =>
        _commands[command.Name] = command;

    private void RegisterDefaultCommands()
    {
        RegisterCommand(new RuzenBot.Command("/man", "Get help", HandleHelpCommand));
        RegisterCommand(new RuzenBot.Command("/sent", "Start program", HandlerShell));
        RegisterCommand(new RuzenBot.Command("/id", "Get ur id", HandlerIdGet));
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
            Error = $"Use /{_commands["/sent"].Name} <arguments>",
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
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/api/command/execute", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<CommandResponse>(responseJson);
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