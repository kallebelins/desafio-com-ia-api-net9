using System.Diagnostics;

namespace DesafioComIA.Application.Telemetry;

/// <summary>
/// Classe centralizada para instrumentação de telemetria.
/// Contém o ActivitySource principal para criação de spans customizados.
/// </summary>
public static class DiagnosticsConfig
{
    /// <summary>
    /// Nome do serviço usado para identificação nos traces.
    /// </summary>
    public const string ServiceName = "DesafioComIA.Api";

    /// <summary>
    /// Versão do serviço.
    /// </summary>
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// ActivitySource principal para instrumentação de operações CQRS.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    /// <summary>
    /// ActivitySource para operações de cache.
    /// </summary>
    public static readonly ActivitySource CacheActivitySource = new($"{ServiceName}.Cache", ServiceVersion);

    /// <summary>
    /// ActivitySource para operações de domínio/negócio.
    /// </summary>
    public static readonly ActivitySource DomainActivitySource = new($"{ServiceName}.Domain", ServiceVersion);
}
