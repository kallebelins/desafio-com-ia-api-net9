# Guia de Cache

Este documento descreve a implementação de cache na aplicação DesafioComIA API utilizando HybridCache do .NET 9.

## Sumário

- [Visão Geral](#visão-geral)
- [Arquitetura de Cache](#arquitetura-de-cache)
- [Configuração](#configuração)
- [Estratégia de Cache](#estratégia-de-cache)
- [Invalidação de Cache](#invalidação-de-cache)
- [Padrão de Chaves](#padrão-de-chaves)
- [Monitoramento](#monitoramento)
- [Endpoint de Diagnóstico](#endpoint-de-diagnóstico)
- [Troubleshooting](#troubleshooting)

---

## Visão Geral

A aplicação utiliza **HybridCache** do .NET 9, que oferece:

- **Cache em dois níveis (L1 + L2)**
- **Proteção contra stampede** (evita múltiplas requisições simultâneas ao banco)
- **Serialização eficiente** com suporte a tipos personalizados
- **Fallback automático** quando Redis está indisponível

### Tecnologias

| Componente | Tecnologia | Descrição |
|------------|------------|-----------|
| L1 Cache | In-Memory | Cache local rápido, por instância |
| L2 Cache | Redis | Cache distribuído, compartilhado entre instâncias |
| Abstração | HybridCache | API unificada do .NET 9 |

---

## Arquitetura de Cache

```
┌─────────────────────────────────────────────────────────────┐
│                        Aplicação                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐                                        │
│  │  Query Handler  │                                        │
│  └────────┬────────┘                                        │
│           │                                                 │
│           ▼                                                 │
│  ┌─────────────────┐                                        │
│  │  ICacheService  │ (HybridCacheService)                   │
│  └────────┬────────┘                                        │
│           │                                                 │
│           ▼                                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                   HybridCache                        │   │
│  │  ┌──────────────┐    ┌──────────────────────────┐   │   │
│  │  │  L1 (Memory) │───▶│  L2 (Redis - opcional)   │   │   │
│  │  │  TTL: 1 min  │    │  TTL: conforme config    │   │   │
│  │  └──────────────┘    └──────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Fluxo de Operação

1. **Cache Hit (L1)**: Retorno imediato da memória
2. **Cache Hit (L2)**: Busca no Redis, armazena em L1, retorna
3. **Cache Miss**: Busca no banco, armazena em L1 e L2, retorna

---

## Configuração

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  },
  "Cache": {
    "Enabled": true,
    "DefaultTTLMinutes": 5,
    "ListClientesTTLMinutes": 5,
    "GetClienteByIdTTLMinutes": 10,
    "SearchClientesTTLMinutes": 3,
    "LocalCacheTTLMinutes": 1,
    "MaximumPayloadBytes": 1048576,
    "MaximumKeyLength": 1024,
    "KeyPrefix": "desafiocomia:"
  }
}
```

### Parâmetros de Configuração

| Parâmetro | Descrição | Recomendação |
|-----------|-----------|--------------|
| `Enabled` | Habilita/desabilita cache globalmente | `true` em produção |
| `DefaultTTLMinutes` | TTL padrão | 5 minutos |
| `ListClientesTTLMinutes` | TTL para listagens | 5 minutos |
| `GetClienteByIdTTLMinutes` | TTL para busca por ID | 10 minutos |
| `SearchClientesTTLMinutes` | TTL para buscas filtradas | 3 minutos |
| `LocalCacheTTLMinutes` | TTL do cache L1 | 1 minuto |
| `KeyPrefix` | Prefixo das chaves | `desafiocomia:` |

---

## Estratégia de Cache

### Onde o Cache é Aplicado

| Operação | Query Handler | Cacheado | TTL |
|----------|---------------|----------|-----|
| Listar clientes | `ListClientesQueryHandler` | Sim | 5 min |
| Buscar clientes | `GetClientesQueryHandler` | Sim | 3 min |
| Obter por ID | `GetClienteByIdQueryHandler` | Sim | 10 min |
| Criar cliente | `CreateClienteCommandHandler` | Não | - |
| Atualizar cliente | `UpdateClienteCommandHandler` | Não | - |
| Atualizar parcial | `PatchClienteCommandHandler` | Não | - |
| Remover cliente | `DeleteClienteCommandHandler` | Não | - |

### Implementação nos Handlers

```csharp
// Exemplo: GetClienteByIdQueryHandler
public async Task<ClienteDto> Handle(GetClienteByIdQuery query, CancellationToken ct)
{
    var cacheKey = CacheKeyHelper.GetClienteByIdKey(query.Id);
    
    return await _cacheService.GetOrCreateAsync(
        cacheKey,
        async () =>
        {
            // Factory executada apenas em cache miss
            var cliente = await _repository.GetByIdAsync(query.Id, ct);
            if (cliente == null)
                throw new ClienteNaoEncontradoException(query.Id);
            return _mapper.Map<ClienteDto>(cliente);
        },
        _cacheSettings.Value.GetClienteByIdTTL);
}
```

---

## Invalidação de Cache

### Estratégia de Invalidação

A invalidação de cache ocorre **após** operações de escrita bem-sucedidas:

| Operação | Invalidações |
|----------|--------------|
| **Criar cliente** | Listagem, Busca |
| **Atualizar cliente** | Cliente específico, Listagem, Busca |
| **Atualizar parcial** | Cliente específico, Listagem, Busca |
| **Remover cliente** | Cliente específico, Listagem, Busca |

### Padrões de Invalidação

```csharp
// Após criar/atualizar/remover cliente
await _cacheService.RemoveByPatternAsync(CacheKeyHelper.GetClientesListPattern());   // clientes:list:*
await _cacheService.RemoveByPatternAsync(CacheKeyHelper.GetClientesSearchPattern()); // clientes:search:*
await _cacheService.RemoveAsync(CacheKeyHelper.GetClienteByIdKey(clienteId));        // clientes:id:{guid}
```

### Resiliência

A invalidação de cache é **não-bloqueante**:

```csharp
try
{
    await _cacheService.RemoveByPatternAsync(pattern);
    _logger.LogInformation("Cache invalidado: {Pattern}", pattern);
}
catch (Exception ex)
{
    // Erro no cache NÃO impede a operação principal
    _logger.LogWarning(ex, "Falha ao invalidar cache: {Pattern}", pattern);
}
```

---

## Padrão de Chaves

### Estrutura das Chaves

```
{prefix}:{domínio}:{operação}:{identificador}
```

### Exemplos

| Tipo | Padrão | Exemplo |
|------|--------|---------|
| Listagem | `{prefix}clientes:list:page{n}:size{n}` | `desafiocomia:clientes:list:page1:size10` |
| Busca | `{prefix}clientes:search:{hash}` | `desafiocomia:clientes:search:a1b2c3d4` |
| Por ID | `{prefix}clientes:id:{guid}` | `desafiocomia:clientes:id:123e4567-...` |

### Helper de Chaves

```csharp
public static class CacheKeyHelper
{
    public static string GetListClientesKey(int page, int pageSize)
        => $"clientes:list:page{page}:size{pageSize}";
    
    public static string GetSearchClientesKey(GetClientesQuery query)
        => $"clientes:search:{ComputeHash(query)}";
    
    public static string GetClienteByIdKey(Guid id)
        => $"clientes:id:{id}";
    
    // Padrões para invalidação
    public static string GetClientesListPattern() => "clientes:list:*";
    public static string GetClientesSearchPattern() => "clientes:search:*";
    public static string GetClientesPattern() => "clientes:*";
}
```

---

## Monitoramento

### Métricas de Cache

O sistema expõe métricas no endpoint `/metrics`:

| Métrica | Tipo | Descrição |
|---------|------|-----------|
| `cache_hits_total` | Counter | Total de cache hits |
| `cache_misses_total` | Counter | Total de cache misses |
| `cache_invalidations_total` | Counter | Total de invalidações |
| `cache_operation_duration_seconds` | Histogram | Duração das operações |

### Queries Prometheus

```promql
# Taxa de hits
rate(cache_hits_total[5m])

# Taxa de misses
rate(cache_misses_total[5m])

# Hit rate (%)
rate(cache_hits_total[5m]) / (rate(cache_hits_total[5m]) + rate(cache_misses_total[5m])) * 100

# Latência média de operações de cache
histogram_quantile(0.95, rate(cache_operation_duration_seconds_bucket[5m]))
```

### Dashboard Grafana

O dashboard "DesafioComIA API Overview" inclui painéis de cache:
- Cache Hit Rate
- Cache Operations (hits, misses, invalidations)
- Operation Duration

---

## Endpoint de Diagnóstico

Em ambiente **Development**, a API expõe endpoints de diagnóstico de cache:

### GET /api/cache/stats

Retorna estatísticas e configuração do cache.

**Response:**
```json
{
  "enabled": true,
  "configuration": {
    "defaultTTLMinutes": 5,
    "listClientesTTLMinutes": 5,
    "getClienteByIdTTLMinutes": 10,
    "searchClientesTTLMinutes": 3
  },
  "redis": {
    "connected": true,
    "endpoint": "localhost:6379"
  }
}
```

### DELETE /api/cache/clear

Limpa todo o cache de clientes.

**Response:** `204 No Content`

### DELETE /api/cache/key/{key}

Remove uma chave específica do cache.

**Exemplo:**
```bash
curl -X DELETE "http://localhost:5001/api/cache/key/clientes:id:123e4567-e89b-12d3-a456-426614174000"
```

---

## Troubleshooting

### Cache não está funcionando

1. **Verificar configuração**: `Cache.Enabled` deve ser `true`
2. **Verificar Redis**: Execute `redis-cli ping` para testar conexão
3. **Verificar logs**: Procure por warnings relacionados a cache

### Redis indisponível

Se o Redis estiver indisponível:
- O sistema continua funcionando
- Apenas cache L1 (memória) será utilizado
- Logs de warning serão emitidos

### Performance degradada

1. **Verifique TTL**: TTL muito baixo causa muitos misses
2. **Verifique métricas**: Analise hit rate no Grafana
3. **Verifique invalidações**: Muitas invalidações podem indicar problema

### Dados desatualizados

1. **Verificar invalidação**: Confirme que handlers estão invalidando cache
2. **Verificar TTL**: Reduza TTL se necessário
3. **Limpar cache manualmente**: Use endpoint `/api/cache/clear`

### Como desabilitar cache para debugging

```json
{
  "Cache": {
    "Enabled": false
  }
}
```

Ou via variável de ambiente:
```bash
Cache__Enabled=false
```
