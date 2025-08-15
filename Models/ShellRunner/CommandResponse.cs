namespace RuzenBot.Models.ShellRunner;

public class CommandResponse
{
    public string Output { get; init; }
    public required CommandRequest Context { get; init; }
    public string Error { get; init; }
    public required int ExitCode { get; init; }
}