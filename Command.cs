using Telegram.Bot.Types;

namespace RuzenBot;

public class Command(string name, string description, Func<Message, Task> handler)
{
    public string Name { get; } = name;
    private string Description { get; } = description;
    public Func<Message, Task> Handler { get; } = handler;
    public string GetMan() => $"{Name} - {Description}";
}