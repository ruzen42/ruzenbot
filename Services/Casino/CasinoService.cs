using RuzenBot.Models.Casino;
using RuzenBot.Services.DbService;
using Microsoft.Extensions.Logging;

namespace RuzenBot.Services.Casino;

public class CasinoService(IBotDbService botDbService, ILogger<CasinoService> logger) : ICasinoService
{
    private readonly Random _random = new();

    public async Task<GameResult> MakeGameAsync(GameRequest models)
    {
        ArgumentNullException.ThrowIfNull(models);

        var (isRegistered, user) = await EnsureUserRegisteredAndGetUserAsync(models.User);
        
        if (!isRegistered)
        {
            logger.LogInformation("Registering new user {UserId}", models.User.Id);
            await RegisterUserAsync(user);
        }

        var gameResult = CalculateGameResult(models.GameType, user);

        if (gameResult.User.Money == user.Money) return gameResult;
        await botDbService.UpdateUser(gameResult.User);
        logger.LogInformation("User {UserId} updated. New balance: {Money}", 
            gameResult.User.Id, gameResult.User.Money);

        return gameResult;
    }

    public async Task<long> GetUserMoneyAsync(long userId) => 
        (await botDbService.GetUser(userId))?.Money ?? 0;

    public async Task RegisterUserAsync(User user)
    {
        try
        {
            await botDbService.CreateUser(user);
            logger.LogInformation("User {UserId} registered successfully", user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<(bool isRegistered, User user)> EnsureUserRegisteredAndGetUserAsync(User user)
    {
        try
        {
            var dbUser = await botDbService.GetUser(user.Id);
            return (dbUser != null, dbUser ?? user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user {UserId} from database", user.Id);
            return (false, user);
        }
    }

    private GameResult CalculateGameResult(GameType gameType, User user)
    {
        var maxValue = gameType switch
        {
            GameType.AllOrNothing or GameType.FiftyFifty => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(gameType), 
                $"Unsupported game type: {gameType}")
        };
        
        var isWin = _random.Next(0, maxValue) == 1;
        var updatedUser = CalculateNewBalance(gameType, user, isWin);

        return new GameResult(isWin, updatedUser);
    }

    private static User CalculateNewBalance(GameType gameType, User user, bool isWin)
    {
        return gameType switch
        {
            GameType.AllOrNothing => isWin 
                ? user with { Money = user.Money * 10 }
                : user with { Money = 0 },
                
            GameType.FiftyFifty => isWin 
                ? user with { Money = user.Money * 3 / 2 }
                : user with { Money = user.Money / 2 },
                
            _ => throw new ArgumentOutOfRangeException(nameof(gameType), 
                $"Unsupported game type: {gameType}")
        };
    }
}

