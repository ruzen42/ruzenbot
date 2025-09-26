namespace RuzenBot.Models.ShellRunner;

public readonly record struct CommandResponse(string Output, string Error, int ExitCode)
{
    public override string ToString() => ExitCode == 0 ? Output : Output + Error;
}
