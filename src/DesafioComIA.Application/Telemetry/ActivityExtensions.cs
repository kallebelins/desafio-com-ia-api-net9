using System.Diagnostics;

namespace DesafioComIA.Application.Telemetry;

/// <summary>
/// Extension methods para Activity/Span com suporte a mascaramento de dados sensíveis.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Define uma tag com mascaramento automático de dados sensíveis.
    /// </summary>
    /// <param name="activity">Activity atual.</param>
    /// <param name="key">Nome da tag.</param>
    /// <param name="value">Valor da tag.</param>
    /// <returns>A Activity para encadeamento.</returns>
    public static Activity? SetTagSafe(this Activity? activity, string key, string? value)
    {
        if (activity == null || value == null)
            return activity;

        var maskedValue = SensitiveDataProcessor.MaskIfSensitive(key, value);
        return activity.SetTag(key, maskedValue);
    }

    /// <summary>
    /// Define uma tag de cliente com mascaramento de dados sensíveis.
    /// </summary>
    public static Activity? SetClienteTag(this Activity? activity, string? nome, string? cpf, string? email)
    {
        activity?.SetTag("cliente.nome", nome);
        activity?.SetTagSafe("cliente.cpf", cpf);
        activity?.SetTagSafe("cliente.email", email);
        return activity;
    }

    /// <summary>
    /// Adiciona informações de cliente a um Activity.
    /// </summary>
    public static Activity? SetClienteId(this Activity? activity, Guid clienteId)
    {
        return activity?.SetTag("cliente.id", clienteId.ToString());
    }

    /// <summary>
    /// Define o status da Activity como erro com mensagem.
    /// </summary>
    public static Activity? SetError(this Activity? activity, Exception exception)
    {
        if (activity == null)
            return null;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        // AddException is the recommended way (RecordException is deprecated)
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName ?? "Unknown" },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace ?? "" }
        }));
        return activity;
    }

    /// <summary>
    /// Define o status da Activity como sucesso.
    /// </summary>
    public static Activity? SetSuccess(this Activity? activity, string? description = null)
    {
        activity?.SetStatus(ActivityStatusCode.Ok, description);
        return activity;
    }

    /// <summary>
    /// Adiciona um evento indicando cache hit.
    /// </summary>
    public static Activity? AddCacheHitEvent(this Activity? activity, string key)
    {
        activity?.AddEvent(new ActivityEvent("CacheHit", tags: new ActivityTagsCollection
        {
            { "cache.key", key }
        }));
        return activity;
    }

    /// <summary>
    /// Adiciona um evento indicando cache miss.
    /// </summary>
    public static Activity? AddCacheMissEvent(this Activity? activity, string key)
    {
        activity?.AddEvent(new ActivityEvent("CacheMiss", tags: new ActivityTagsCollection
        {
            { "cache.key", key }
        }));
        return activity;
    }

    /// <summary>
    /// Adiciona um evento de invalidação de cache.
    /// </summary>
    public static Activity? AddCacheInvalidationEvent(this Activity? activity, string pattern)
    {
        activity?.AddEvent(new ActivityEvent("CacheInvalidation", tags: new ActivityTagsCollection
        {
            { "cache.pattern", pattern }
        }));
        return activity;
    }
}
