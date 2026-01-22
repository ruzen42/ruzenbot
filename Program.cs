using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoSimpleLogger;
using RuzenBot.Services;
using RuzenBot.Services.Bot;
using RuzenBot.Services.CallbackQuery;
using RuzenBot.Services.Casino;
using RuzenBot.Services.Command;
using RuzenBot.Services.DbService;
using RuzenBot.Services.GithubApi;
using RuzenBot.Services.Message;
using RuzenBot.Services.QueryInlineHandler;
using RuzenBot.Services.ShellRunner;
using RuzenBot.Services.Update;
using Telegram.Bot;
using Telegram.Bot.Polling;
using CommandLine; 

namespace RuzenBot;

internal static class Program
{
    private class Options
    {
        [Option('m', "no-microservices", Required = false, HelpText = "Not use microservices (default use).", Default = true)]
        public bool MServicesOn { get; set; } = true;
    }
    
    private static async Task Main(string[] args)
    {
        var tokenTelegram = Environment.GetEnvironmentVariable("TOKEN");
        if (string.IsNullOrWhiteSpace(tokenTelegram))
        {
            Console.WriteLine("Error: token is null or empty");
            return;
        }

        IHost? host = null;
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
                host = o.MServicesOn
                    ? CreateHostBuilder(tokenTelegram).Build()
                    : CreateHostBuilder(tokenTelegram, false).Build());
        if (host != null) await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string token, bool useMicroServices = true) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITelegramBotClient>(_ =>
                    new TelegramBotClient(token));
                if (useMicroServices)
                {
                    services.AddSingleton<IQueryInlineHandler, QueryInlineHandler>();
                    services.AddSingleton<IBotDbService, BotDbService>();
                    services.AddSingleton<ICasinoService, CasinoService>();
                    services.AddHttpClient<IShellRunnerService, ShellRunnerService>(client => client.Timeout = TimeSpan.FromSeconds(10));
                    services.AddSingleton<IGithubApiService, GithubApiService>(); 
                    services.AddSingleton<IShellRunnerService, ShellRunnerService>(); 
                }
                services.AddSingleton<ICommandService, CommandService>();
                services.AddSingleton<IMessageHandler, MessageHandler>();
                services.AddSingleton<ICallbackQueryHandler, CallbackQueryHandler>();
                services.AddSingleton<IUpdateHandler, UpdateHandler>();
                services.AddSingleton<ILogger, Logger>();
                services.AddHostedService<BotHostedService>();

                services.AddSingleton<IBotService, BotService>();

                //services.AddHostedService<ConsoleHostedService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new LoggerProvider());
            });
}
