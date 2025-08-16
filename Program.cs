using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoSimpleLogger;
using RuzenBot.Services;
using RuzenBot.Services.Bot;
using RuzenBot.Services.CallbackQuery;
using RuzenBot.Services.Command;
using RuzenBot.Services.Message;
using RuzenBot.Services.ShellRunnerExecute;
using RuzenBot.Services.Update;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace RuzenBot;

internal static class Program
{
    private static async Task Main()
    {
        var tokenTelegram = Environment.GetEnvironmentVariable("TOKEN") ?? "test";
        var host = CreateHostBuilder(tokenTelegram).Build();
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string token) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((services) =>
            {
                services.AddSingleton<ITelegramBotClient>(_ =>
                    new TelegramBotClient(token));

                services.AddSingleton<ICommandService, CommandService>();
                services.AddSingleton<IMessageHandler, MessageHandler>();
                services.AddSingleton<ICallbackQueryHandler, CallbackQueryHandler>();
                services.AddSingleton<IUpdateHandler, UpdateHandler>();
                services.AddSingleton<ILogger, Logger>();
                services.AddSingleton<IShellRunnerHttp, ShellRunnerHttp>(); 

                services.AddSingleton<IBotService, BotService>();

                services.AddHostedService<BotHostedService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new LoggerProvider());
            });
}