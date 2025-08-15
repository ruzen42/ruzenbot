
namespace RuzenBot.Services.Bot;

public interface IBotService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
}