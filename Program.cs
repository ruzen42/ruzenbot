using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoSimpleLogger;
using RuzenBot.Services;
using Telegram.Bot;

namespace RuzenBot;

internal static class Program
{
    public static Logger logger = new(Logger.TypeLogger.Console)
    {
        WarnColor = ConsoleColor.Cyan
    };

    private static async Task Main(string[] args)
    {
        var token = Environment.GetEnvironmentVariable("TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            logger.Fatal("TOKEN environment variable is not set");
            return;
        }

        try
        {
            var host = CreateHostBuilder(args, token).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            logger.Fatal($"Critical error: {ex}");
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, string token) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<ITelegramBotClient>(provider => 
                    new TelegramBotClient(token));

                services.AddSingleton<ICommandService, CommandService>();
                services.AddSingleton<IMessageHandler, MessageHandler>();
                services.AddSingleton<ICallbackQueryHandler, CallbackQueryHandler>();
                services.AddSingleton<IUpdateHandler, UpdateHandler>();

                services.AddSingleton<IBotService, BotService>();

                services.AddHostedService<BotHostedService>();
            });
}