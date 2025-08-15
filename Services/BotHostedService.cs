using Microsoft.Extensions.Hosting;
using RuzenBot.Services.Bot;

namespace RuzenBot.Services;

public class BotHostedService(IBotService botService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await botService.StartAsync(stoppingToken);
        await Task.Delay(5000, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await botService.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}