using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoSimpleLogger;
using RuzenBot.Services;
using RuzenBot.Services.Bot;
using RuzenBot.Services.CallbackQuery;
using RuzenBot.Services.Casino;
using RuzenBot.Services.Command;
using RuzenBot.Services.ConsoleCommand;
using RuzenBot.Services.DbService;
using RuzenBot.Services.GithubApi;
using RuzenBot.Services.Message;
using RuzenBot.Services.QueryInlineHandler;
using RuzenBot.Services.ShellRunner;
using RuzenBot.Services.Update;
using Telegram.Bot;
using Telegram.Bot.Polling;

var token = Environment.GetEnvironmentVariable("TOKEN");

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Error: TOKEN environment variable is not set.");
    return;
}

var useMicroServices = !args.Contains("--no-microservices");

var host = CreateHostBuilder(token, useMicroServices).Build();

await host.RunAsync();
return;

static IHostBuilder CreateHostBuilder(string token, bool useMicroServices) =>
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

                services.AddHttpClient<IShellRunnerService, ShellRunnerService>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                });

                services.AddSingleton<IGithubApiService, GithubApiService>();
                services.AddSingleton<IShellRunnerService, ShellRunnerService>();
            }

            services.AddSingleton<ICommandService, CommandService>();
            services.AddSingleton<IConsoleService, ConsoleService>();
            services.AddSingleton<IMessageHandler, MessageHandler>();
            services.AddSingleton<ICallbackQueryHandler, CallbackQueryHandler>();
            services.AddSingleton<IUpdateHandler, UpdateHandler>();
            services.AddSingleton<ILogger, Logger>();

            services.AddSingleton<IBotService, BotService>();

            services.AddHostedService<BotHostedService>();
            services.AddHostedService<ConsoleHostedService>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new LoggerProvider());
        });
