namespace DesafioComIA.Api.Configuration;

/// <summary>
/// Configurações do OpenTelemetry.
/// </summary>
public class OpenTelemetrySettings
{
    /// <summary>
    /// Nome da seção no appsettings.json.
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// Nome do serviço para identificação nos traces e métricas.
    /// </summary>
    public string ServiceName { get; set; } = "DesafioComIA.Api";

    /// <summary>
    /// Versão do serviço.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Habilita o exportador de console (apenas para desenvolvimento).
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = false;

    /// <summary>
    /// Configurações do OTLP (OpenTelemetry Protocol).
    /// </summary>
    public OtlpSettings Otlp { get; set; } = new();

    /// <summary>
    /// Configurações de tracing.
    /// </summary>
    public TracingSettings Tracing { get; set; } = new();

    /// <summary>
    /// Configurações de métricas.
    /// </summary>
    public MetricsSettings Metrics { get; set; } = new();

    /// <summary>
    /// Configurações de logging.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();
}

/// <summary>
/// Configurações do OTLP (OpenTelemetry Protocol).
/// </summary>
public class OtlpSettings
{
    /// <summary>
    /// Endpoint do coletor OTLP (ex: http://localhost:4317 para gRPC).
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Protocolo de comunicação (Grpc ou HttpProtobuf).
    /// </summary>
    public string Protocol { get; set; } = "Grpc";
}

/// <summary>
/// Configurações de tracing distribuído.
/// </summary>
public class TracingSettings
{
    /// <summary>
    /// Habilita tracing.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Probabilidade de sampling (0.0 a 1.0). 1.0 = 100% das requisições são trackeadas.
    /// </summary>
    public double SamplingProbability { get; set; } = 1.0;
}

/// <summary>
/// Configurações de métricas.
/// </summary>
public class MetricsSettings
{
    /// <summary>
    /// Habilita métricas.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Endpoint do Prometheus para scraping de métricas.
    /// </summary>
    public string PrometheusEndpoint { get; set; } = "/metrics";
}

/// <summary>
/// Configurações de logging estruturado.
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Habilita logging via OpenTelemetry.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Inclui mensagem formatada nos logs.
    /// </summary>
    public bool IncludeFormattedMessage { get; set; } = true;

    /// <summary>
    /// Inclui scopes nos logs.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;
}
