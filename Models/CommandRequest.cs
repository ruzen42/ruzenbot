namespace RuzenBot.Models;

public class CommandRequest
{
    public string Command { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
}