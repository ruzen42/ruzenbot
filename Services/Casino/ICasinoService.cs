using RuzenBot.Models.Casino;

namespace RuzenBot.Services.Casino;

public interface ICasinoService
{
    Task<GameResult> MakeGameAsync(GameRequest request);
    Task RegisterUserAsync(User user);
    Task<(bool isRegistered, User user)> EnsureUserRegisteredAndGetUserAsync(User user);
    Task<long> GetUserMoneyAsync(long userId);
}