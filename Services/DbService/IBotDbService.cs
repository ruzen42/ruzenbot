using RuzenBot.Models.Casino;

namespace RuzenBot.Services.DbService;

public interface IBotDbService
{
    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Найденный пользователь</returns>
    Task<User?> GetUser(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить всех пользователей
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список всех пользователей</returns>
    Task<List<User>?> GetUsers(CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить пользователя по ID
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DeleteUser(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    /// <param name="user">Данные пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task CreateUser(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить данные пользователя
    /// </summary>
    /// <param name="user">Обновленные данные пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task UpdateUser(User user, CancellationToken cancellationToken = default);
}