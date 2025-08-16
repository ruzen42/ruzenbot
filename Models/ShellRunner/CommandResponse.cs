namespace RuzenBot.Models.ShellRunner;

public class CommandResponse
{
    public string Output { get; init; } 
    public string Error { get; init; }
    public required int ExitCode { get; init; }

    public override string ToString() => ExitCode == 0 ? Output : Output + Error;
}
