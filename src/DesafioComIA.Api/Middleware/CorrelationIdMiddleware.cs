using System.Diagnostics;

namespace DesafioComIA.Api.Middleware;

/// <summary>
/// Middleware para propagação de Correlation ID nas requisições.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// Nome do header de Correlation ID.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// Cria uma nova instância do middleware.
    /// </summary>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processa a requisição adicionando Correlation ID.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Adiciona ao Activity atual para propagação em traces
        Activity.Current?.SetTag("correlation.id", correlationId);

        // Adiciona ao response header
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        // Adiciona ao logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Tenta extrair do header da requisição
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) 
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Usa o TraceId do Activity se disponível
        if (Activity.Current?.TraceId.ToString() is { } traceId && !string.IsNullOrEmpty(traceId))
        {
            return traceId;
        }

        // Gera um novo ID
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension methods para configuração do CorrelationIdMiddleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adiciona o middleware de Correlation ID ao pipeline.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
