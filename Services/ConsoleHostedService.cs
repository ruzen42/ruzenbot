using Microsoft.Extensions.Hosting;
using RuzenBot.Services.ConsoleCommand;

namespace RuzenBot.Services;

public class ConsoleHostedService(IConsoleService consoleService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await consoleService.StartAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await consoleService.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}