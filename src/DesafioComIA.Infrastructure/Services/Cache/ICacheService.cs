namespace DesafioComIA.Infrastructure.Services.Cache;

/// <summary>
/// Interface para serviço de cache da aplicação
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtém um valor do cache ou executa a factory se não existir
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="factory">Factory para criar o valor se não existir no cache</param>
    /// <param name="expiration">Tempo de expiração (opcional)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Valor do cache ou resultado da factory</returns>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um valor do cache
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Valor do cache ou default se não existir</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Define um valor no cache
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="value">Valor a ser armazenado</param>
    /// <param name="expiration">Tempo de expiração (opcional)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um valor específico do cache
    /// </summary>
    /// <param name="key">Chave do cache</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove múltiplas chaves do cache
    /// </summary>
    /// <param name="keys">Lista de chaves a serem removidas</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove todas as chaves que correspondem ao padrão especificado
    /// </summary>
    /// <param name="pattern">Padrão para matching (ex: "clientes:list:*")</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se o cache está habilitado
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Obtém lista de chaves rastreadas (para debug)
    /// </summary>
    /// <returns>Lista de chaves atualmente rastreadas</returns>
    IReadOnlyCollection<string> GetTrackedKeys();
}
