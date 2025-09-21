namespace RuzenBot.Services.ConsoleCommand;

public interface IConsoleService
{
   Task StartAsync(CancellationToken cancellationToken = default);
   Task StopAsync(CancellationToken cancellationToken = default);
}