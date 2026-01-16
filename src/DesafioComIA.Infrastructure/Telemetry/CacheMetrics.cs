using System.Diagnostics.Metrics;

namespace DesafioComIA.Infrastructure.Telemetry;

/// <summary>
/// Métricas customizadas para operações de cache.
/// </summary>
public class CacheMetrics
{
    /// <summary>
    /// Nome do meter para métricas de cache.
    /// </summary>
    public const string MeterName = "DesafioComIA.Cache";

    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _cacheInvalidations;
    private readonly Histogram<double> _cacheOperationDuration;

    /// <summary>
    /// Cria uma nova instância de CacheMetrics.
    /// </summary>
    /// <param name="meterFactory">Factory de meters do OpenTelemetry.</param>
    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _cacheHits = meter.CreateCounter<long>(
            name: "cache.hits",
            unit: "{hit}",
            description: "Total de cache hits");

        _cacheMisses = meter.CreateCounter<long>(
            name: "cache.misses",
            unit: "{miss}",
            description: "Total de cache misses");

        _cacheInvalidations = meter.CreateCounter<long>(
            name: "cache.invalidations",
            unit: "{invalidation}",
            description: "Total de invalidações de cache");

        _cacheOperationDuration = meter.CreateHistogram<double>(
            name: "cache.operation.duration",
            unit: "ms",
            description: "Duração das operações de cache em milissegundos");
    }

    /// <summary>
    /// Registra um cache hit.
    /// </summary>
    /// <param name="keyPattern">Padrão da chave acessada.</param>
    public void CacheHit(string keyPattern = "unknown") =>
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache.key_pattern", keyPattern));

    /// <summary>
    /// Registra um cache miss.
    /// </summary>
    /// <param name="keyPattern">Padrão da chave acessada.</param>
    public void CacheMiss(string keyPattern = "unknown") =>
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache.key_pattern", keyPattern));

    /// <summary>
    /// Registra uma invalidação de cache.
    /// </summary>
    /// <param name="pattern">Padrão de chaves invalidadas.</param>
    public void CacheInvalidation(string pattern = "unknown") =>
        _cacheInvalidations.Add(1, new KeyValuePair<string, object?>("cache.pattern", pattern));

    /// <summary>
    /// Registra a duração de uma operação de cache.
    /// </summary>
    /// <param name="milliseconds">Duração em milissegundos.</param>
    /// <param name="operation">Nome da operação (get, set, remove).</param>
    public void RecordOperationDuration(double milliseconds, string operation) =>
        _cacheOperationDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("cache.operation", operation));
}
