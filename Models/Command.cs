using Telegram.Bot.Types;

namespace RuzenBot.Models;

public readonly record struct Command(string Name, string Description, Func<Message, CancellationToken, Task> Handler)
{
    public override string ToString() => $"{Name} - {Description}";
}