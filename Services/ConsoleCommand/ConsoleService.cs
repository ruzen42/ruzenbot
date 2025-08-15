namespace RuzenBot.Services.ConsoleCommand;

public class ConsoleService : IConsoleService
{
    public Task<string> ReadAsync(CancellationToken cancellationToken)
    {
        var output = Console.ReadLine();
        return Task.FromResult(output);
    }
}