namespace RuzenBot.Models.ShellRunner;

public struct Url
{
    public required string Host;
    public required string Path;
    public required string Port;

    public override string ToString() => $"{Host}:{Port}{Path}";
}