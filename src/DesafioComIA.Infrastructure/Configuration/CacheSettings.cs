namespace DesafioComIA.Infrastructure.Configuration;

/// <summary>
/// Configurações de cache da aplicação
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Seção de configuração no appsettings.json
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Indica se o cache está habilitado
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// TTL padrão em minutos para entradas de cache
    /// </summary>
    public int DefaultTTLMinutes { get; set; } = 5;

    /// <summary>
    /// TTL em minutos para listagem de clientes
    /// </summary>
    public int ListClientesTTLMinutes { get; set; } = 5;

    /// <summary>
    /// TTL em minutos para busca de cliente por ID
    /// </summary>
    public int GetClienteByIdTTLMinutes { get; set; } = 10;

    /// <summary>
    /// TTL em minutos para busca/search de clientes
    /// </summary>
    public int SearchClientesTTLMinutes { get; set; } = 3;

    /// <summary>
    /// TTL em minutos para cache local (L1) - deve ser menor que o TTL total
    /// </summary>
    public int LocalCacheTTLMinutes { get; set; } = 1;

    /// <summary>
    /// Tamanho máximo em bytes para entradas no cache (padrão: 1MB)
    /// </summary>
    public int MaximumPayloadBytes { get; set; } = 1024 * 1024;

    /// <summary>
    /// Tamanho máximo da chave de cache
    /// </summary>
    public int MaximumKeyLength { get; set; } = 1024;

    /// <summary>
    /// Prefixo para todas as chaves de cache
    /// </summary>
    public string KeyPrefix { get; set; } = "desafiocomia:";
}
