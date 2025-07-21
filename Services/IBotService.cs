
namespace RuzenBot.Services;

public interface IBotService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}