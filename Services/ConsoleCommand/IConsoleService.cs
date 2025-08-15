namespace RuzenBot.Services.ConsoleCommand;

public interface IConsoleService
{
   public Task<string> ReadAsync(CancellationToken cancellationToken); 
}