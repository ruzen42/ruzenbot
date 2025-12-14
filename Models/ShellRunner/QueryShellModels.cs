namespace RuzenBot.Models.ShellRunner;

public record QueryShellRequest(string Command);
public record QueryShellResponse(string? Output, string? Error, int ExitCode);
