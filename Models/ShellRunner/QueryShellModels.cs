namespace RuzenBot.Models.ShellRunner;

public record QueryShellRequest(string Command);
// Use Err instead of Error because microservice use err
public record QueryShellResponse(string? Output, int ExitCode, string? Err); 
