using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoSimpleLogger;
using RuzenBot.Services;
using Telegram.Bot;

namespace RuzenBot;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var tokenTelegram = Environment.GetEnvironmentVariable("TOKEN");

        var host = CreateHostBuilder(args, tokenTelegram).Build();
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args, string token) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<ITelegramBotClient>(_ =>
                    new TelegramBotClient(token));

                services.AddSingleton<ICommandService, CommandService>();
                services.AddSingleton<IMessageHandler, MessageHandler>();
                services.AddSingleton<ICallbackQueryHandler, CallbackQueryHandler>();
                services.AddSingleton<IUpdateHandler, UpdateHandler>();
                services.AddSingleton<ILogger>(_ => new Logger(Logger.OutputType.Console));

                services.AddSingleton<IBotService, BotService>();

                services.AddHostedService<BotHostedService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            
                logging.AddProvider(new LoggerProvider());
            });
}