using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DesafioComIA.Infrastructure.Configuration;
using DesafioComIA.Infrastructure.Services.Cache;
using StackExchange.Redis;

namespace DesafioComIA.Infrastructure.Caching;

/// <summary>
/// Implementação do serviço de cache usando HybridCache do .NET 9
/// </summary>
public class HybridCacheService : ICacheService
{
    private readonly HybridCache _cache;
    private readonly CacheSettings _settings;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly IConnectionMultiplexer? _redis;

    // Registro de chaves para invalidação por padrão (fallback quando Redis não está disponível)
    private readonly ConcurrentDictionary<string, DateTime> _keyRegistry = new();

    public HybridCacheService(
        HybridCache cache,
        IOptions<CacheSettings> settings,
        ILogger<HybridCacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
        _redis = redis;
    }

    /// <inheritdoc />
    public bool IsEnabled => _settings.Enabled;

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Cache desabilitado, executando factory diretamente para chave {Key}", key);
            return await factory(cancellationToken);
        }

        var fullKey = GetFullKey(key);
        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_settings.DefaultTTLMinutes);
        var localExpiration = TimeSpan.FromMinutes(_settings.LocalCacheTTLMinutes);

        try
        {
            var options = new HybridCacheEntryOptions
            {
                Expiration = cacheExpiration,
                LocalCacheExpiration = localExpiration
            };

            // Converter Task<T> para ValueTask<T> para compatibilidade com HybridCache
            var result = await _cache.GetOrCreateAsync(
                fullKey,
                async (ct) => await factory(ct),
                options,
                cancellationToken: cancellationToken);

            // Registrar chave para invalidação
            TrackKey(fullKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao acessar cache para chave {Key}, executando factory diretamente", fullKey);
            return await factory(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return default;
        }

        var fullKey = GetFullKey(key);

        try
        {
            // HybridCache não tem um GetAsync direto, usamos GetOrCreateAsync com factory que retorna default
            // Se o valor existir, retorna ele; se não, retorna default sem armazenar
            var result = await _cache.GetOrCreateAsync<T?>(
                fullKey,
                ct => ValueTask.FromResult<T?>(default),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.Zero, // Não queremos criar nova entrada
                    LocalCacheExpiration = TimeSpan.Zero
                },
                cancellationToken: cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter valor do cache para chave {Key}", fullKey);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var fullKey = GetFullKey(key);
        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_settings.DefaultTTLMinutes);
        var localExpiration = TimeSpan.FromMinutes(_settings.LocalCacheTTLMinutes);

        try
        {
            var options = new HybridCacheEntryOptions
            {
                Expiration = cacheExpiration,
                LocalCacheExpiration = localExpiration
            };

            await _cache.SetAsync(fullKey, value, options, cancellationToken: cancellationToken);

            // Registrar chave para invalidação
            TrackKey(fullKey);

            _logger.LogDebug("Valor armazenado no cache para chave {Key} com expiração de {Expiration}", fullKey, cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao armazenar valor no cache para chave {Key}", fullKey);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var fullKey = GetFullKey(key);

        try
        {
            await _cache.RemoveAsync(fullKey, cancellationToken);
            UntrackKey(fullKey);
            _logger.LogDebug("Chave {Key} removida do cache", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover chave {Key} do cache", fullKey);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        foreach (var key in keys)
        {
            await RemoveAsync(key, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var fullPattern = GetFullKey(pattern);
        var removedCount = 0;

        _logger.LogDebug("Iniciando remoção de chaves com padrão {Pattern} (full: {FullPattern})", pattern, fullPattern);
        _logger.LogDebug("Chaves registradas no momento: {Count}", _keyRegistry.Count);

        try
        {
            // Sempre limpar do registro local primeiro (para limpar o cache L1/memória do HybridCache)
            var keysToRemove = _keyRegistry.Keys
                .Where(k => MatchesPattern(k, fullPattern))
                .ToList();

            _logger.LogDebug("Encontradas {Count} chaves locais matching padrão {Pattern}", keysToRemove.Count, fullPattern);

            foreach (var key in keysToRemove)
            {
                _logger.LogDebug("Removendo chave do HybridCache: {Key}", key);
                await _cache.RemoveAsync(key, cancellationToken);
                UntrackKey(key);
                removedCount++;
            }

            // Se Redis está disponível, também limpar de lá (para garantir L2)
            if (_redis != null && _redis.IsConnected)
            {
                var redisRemoved = await RemoveByPatternRedisAsync(fullPattern, cancellationToken);
                _logger.LogDebug("Removidas {Count} chaves adicionais do Redis", redisRemoved);
            }

            _logger.LogInformation("Removidas {Count} chaves do cache matching padrão {Pattern}", removedCount, fullPattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover chaves por padrão {Pattern} do cache", fullPattern);
        }
    }

    /// <summary>
    /// Remove chaves por padrão usando Redis SCAN
    /// </summary>
    private async Task<int> RemoveByPatternRedisAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (_redis == null)
        {
            return 0;
        }

        var removedCount = 0;

        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServers().FirstOrDefault();

            if (server == null)
            {
                _logger.LogWarning("Nenhum servidor Redis disponível para scan de padrão");
                return 0;
            }

            _logger.LogDebug("Buscando chaves Redis com padrão: {Pattern}", pattern);

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _logger.LogDebug("Encontrada chave Redis: {Key}", key);
                await database.KeyDeleteAsync(key);
                
                // Também remover do HybridCache L1 (a chave no Redis pode ter formato diferente)
                var keyString = key.ToString();
                if (keyString != null)
                {
                    await _cache.RemoveAsync(keyString, cancellationToken);
                    UntrackKey(keyString);
                }
                
                removedCount++;
            }

            _logger.LogDebug("Removidas {Count} chaves do Redis matching padrão {Pattern}", removedCount, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover chaves por padrão {Pattern} do Redis", pattern);
        }

        return removedCount;
    }

    /// <summary>
    /// Obtém a chave completa com prefixo
    /// </summary>
    private string GetFullKey(string key)
    {
        return $"{_settings.KeyPrefix}{key}";
    }

    /// <summary>
    /// Registra uma chave para rastreamento
    /// </summary>
    private void TrackKey(string key)
    {
        _keyRegistry.TryAdd(key, DateTime.UtcNow);
    }

    /// <summary>
    /// Remove uma chave do registro
    /// </summary>
    private void UntrackKey(string key)
    {
        _keyRegistry.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetTrackedKeys()
    {
        return _keyRegistry.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Verifica se uma chave corresponde ao padrão (suporta * como wildcard)
    /// </summary>
    private static bool MatchesPattern(string key, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // Padrão simples: substitui * por regex .*
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
    }
}
