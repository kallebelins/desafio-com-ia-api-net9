using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using DesafioComIA.Infrastructure.Configuration;
using DesafioComIA.Infrastructure.Services.Cache;

namespace DesafioComIA.Api.Controllers;

/// <summary>
/// Controller para diagnóstico e gerenciamento de cache (apenas em Development)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly IConnectionMultiplexer? _redis;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        IWebHostEnvironment environment,
        ILogger<CacheController> logger,
        IConnectionMultiplexer? redis = null)
    {
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _redis = redis;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Obtém estatísticas e status do cache
    /// </summary>
    /// <remarks>
    /// Disponível apenas em ambiente de desenvolvimento.
    /// Retorna informações sobre a conexão Redis e configurações do cache.
    /// </remarks>
    /// <returns>Estatísticas do cache</returns>
    /// <response code="200">Estatísticas retornadas com sucesso</response>
    /// <response code="403">Endpoint não disponível em ambiente de produção</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult GetStats()
    {
        if (!_environment.IsDevelopment())
        {
            return Problem(
                title: "Endpoint não disponível",
                detail: "Este endpoint está disponível apenas em ambiente de desenvolvimento.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var response = new CacheStatsResponse
        {
            CacheEnabled = _cacheSettings.Enabled,
            Settings = new CacheSettingsInfo
            {
                DefaultTTLMinutes = _cacheSettings.DefaultTTLMinutes,
                ListClientesTTLMinutes = _cacheSettings.ListClientesTTLMinutes,
                GetClienteByIdTTLMinutes = _cacheSettings.GetClienteByIdTTLMinutes,
                SearchClientesTTLMinutes = _cacheSettings.SearchClientesTTLMinutes,
                LocalCacheTTLMinutes = _cacheSettings.LocalCacheTTLMinutes,
                KeyPrefix = _cacheSettings.KeyPrefix
            },
            RedisConnected = _redis?.IsConnected ?? false,
            RedisEndpoints = _redis?.GetEndPoints().Select(e => e.ToString()).ToList() ?? new List<string>()
        };

        // Adicionar informações do Redis se conectado
        if (_redis?.IsConnected == true)
        {
            try
            {
                var server = _redis.GetServers().FirstOrDefault();
                if (server != null)
                {
                    response.RedisServerInfo = new RedisServerInfo
                    {
                        Version = server.Version.ToString(),
                        IsConnected = server.IsConnected,
                        IsSlave = server.IsReplica
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao obter informações do servidor Redis");
            }
        }

        return Ok(response);
    }

    /// <summary>
    /// Lista as chaves rastreadas no cache (para debug)
    /// </summary>
    /// <remarks>
    /// Disponível apenas em ambiente de desenvolvimento.
    /// Retorna a lista de chaves atualmente rastreadas pelo serviço de cache.
    /// </remarks>
    /// <returns>Lista de chaves</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    /// <response code="403">Endpoint não disponível em ambiente de produção</response>
    [HttpGet("keys")]
    [ProducesResponseType(typeof(CacheKeysResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult GetTrackedKeys()
    {
        if (!_environment.IsDevelopment())
        {
            return Problem(
                title: "Endpoint não disponível",
                detail: "Este endpoint está disponível apenas em ambiente de desenvolvimento.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var keys = _cacheService.GetTrackedKeys();
        var pattern = $"{_cacheSettings.KeyPrefix}{CacheKeyHelper.GetClientesPattern()}";

        return Ok(new CacheKeysResponse
        {
            TotalKeys = keys.Count,
            Keys = keys.ToList(),
            KeyPrefix = _cacheSettings.KeyPrefix,
            ClientesPattern = pattern
        });
    }

    /// <summary>
    /// Limpa todo o cache de clientes
    /// </summary>
    /// <remarks>
    /// Disponível apenas em ambiente de desenvolvimento.
    /// Remove todas as entradas de cache relacionadas a clientes.
    /// </remarks>
    /// <returns>Confirmação da operação</returns>
    /// <response code="200">Cache limpo com sucesso</response>
    /// <response code="403">Endpoint não disponível em ambiente de produção</response>
    /// <response code="500">Erro ao limpar cache</response>
    [HttpDelete("clear")]
    [ProducesResponseType(typeof(CacheClearResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return Problem(
                title: "Endpoint não disponível",
                detail: "Este endpoint está disponível apenas em ambiente de desenvolvimento.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        try
        {
            _logger.LogInformation("Iniciando limpeza do cache de clientes");

            // Limpar todo o cache de clientes
            await _cacheService.RemoveByPatternAsync(CacheKeyHelper.GetClientesPattern(), cancellationToken);

            _logger.LogInformation("Cache de clientes limpo com sucesso");

            return Ok(new CacheClearResponse
            {
                Success = true,
                Message = "Cache de clientes limpo com sucesso",
                Pattern = CacheKeyHelper.GetClientesPattern(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar cache de clientes");
            return Problem(
                title: "Erro ao limpar cache",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Limpa uma chave específica do cache
    /// </summary>
    /// <param name="key">Chave a ser removida (sem prefixo)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação da operação</returns>
    /// <response code="200">Chave removida com sucesso</response>
    /// <response code="400">Chave não informada</response>
    /// <response code="403">Endpoint não disponível em ambiente de produção</response>
    [HttpDelete("key/{key}")]
    [ProducesResponseType(typeof(CacheClearResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveKey(string key, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return Problem(
                title: "Endpoint não disponível",
                detail: "Este endpoint está disponível apenas em ambiente de desenvolvimento.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return Problem(
                title: "Chave inválida",
                detail: "A chave do cache não pode ser vazia.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            _logger.LogInformation("Removendo chave {Key} do cache", key);

            await _cacheService.RemoveAsync(key, cancellationToken);

            return Ok(new CacheClearResponse
            {
                Success = true,
                Message = $"Chave '{key}' removida do cache",
                Pattern = key,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover chave {Key} do cache", key);
            return Problem(
                title: "Erro ao remover chave",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

#region Response DTOs

/// <summary>
/// Resposta com estatísticas do cache
/// </summary>
public class CacheStatsResponse
{
    /// <summary>
    /// Indica se o cache está habilitado
    /// </summary>
    public bool CacheEnabled { get; set; }

    /// <summary>
    /// Configurações atuais do cache
    /// </summary>
    public CacheSettingsInfo Settings { get; set; } = new();

    /// <summary>
    /// Indica se o Redis está conectado
    /// </summary>
    public bool RedisConnected { get; set; }

    /// <summary>
    /// Lista de endpoints do Redis
    /// </summary>
    public List<string> RedisEndpoints { get; set; } = new();

    /// <summary>
    /// Informações do servidor Redis
    /// </summary>
    public RedisServerInfo? RedisServerInfo { get; set; }
}

/// <summary>
/// Informações das configurações de cache
/// </summary>
public class CacheSettingsInfo
{
    public int DefaultTTLMinutes { get; set; }
    public int ListClientesTTLMinutes { get; set; }
    public int GetClienteByIdTTLMinutes { get; set; }
    public int SearchClientesTTLMinutes { get; set; }
    public int LocalCacheTTLMinutes { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
}

/// <summary>
/// Informações do servidor Redis
/// </summary>
public class RedisServerInfo
{
    public string Version { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsSlave { get; set; }
}

/// <summary>
/// Resposta com lista de chaves rastreadas
/// </summary>
public class CacheKeysResponse
{
    /// <summary>
    /// Total de chaves rastreadas
    /// </summary>
    public int TotalKeys { get; set; }

    /// <summary>
    /// Lista de chaves
    /// </summary>
    public List<string> Keys { get; set; } = new();

    /// <summary>
    /// Prefixo configurado
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Padrão para clientes
    /// </summary>
    public string ClientesPattern { get; set; } = string.Empty;
}

/// <summary>
/// Resposta da operação de limpeza de cache
/// </summary>
public class CacheClearResponse
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem descritiva
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Padrão utilizado para limpeza
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Data/hora da operação
    /// </summary>
    public DateTime Timestamp { get; set; }
}

#endregion
