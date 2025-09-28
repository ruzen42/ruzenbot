using RuzenBot.Services.Casino;

namespace RuzenBot.Models.Casino;

public record GameRequest(User User, long Amount, GameType GameType);
public record GameResult(bool IsWin, User User);

public enum GameType { AllOrNothing, FiftyFifty }