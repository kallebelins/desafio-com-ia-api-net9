# Guia de Observabilidade

Este documento descreve a implementação de observabilidade na aplicação DesafioComIA API utilizando OpenTelemetry.

## Sumário

- [Visão Geral](#visão-geral)
- [Arquitetura de Observabilidade](#arquitetura-de-observabilidade)
- [Componentes](#componentes)
- [Configuração](#configuração)
- [Logs](#logs)
- [Traces](#traces)
- [Métricas](#métricas)
- [Ferramentas de Visualização](#ferramentas-de-visualização)
- [Queries Úteis](#queries-úteis)
- [Mascaramento de Dados Sensíveis](#mascaramento-de-dados-sensíveis)

---

## Visão Geral

A aplicação implementa os **três pilares da observabilidade**:

| Pilar | Tecnologia | Propósito |
|-------|------------|-----------|
| **Logs** | OpenTelemetry Logging | Registros estruturados de eventos |
| **Traces** | OpenTelemetry Tracing | Rastreamento de requisições distribuídas |
| **Métricas** | OpenTelemetry Metrics | Medições de performance e negócio |

### Stack de Observabilidade

| Ferramenta | Porta | URL | Propósito |
|------------|-------|-----|-----------|
| **Jaeger** | 16686 | http://localhost:16686 | Visualização de traces |
| **Prometheus** | 9090 | http://localhost:9090 | Coleta e armazenamento de métricas |
| **Grafana** | 3000 | http://localhost:3000 | Dashboards e visualização |
| **API Metrics** | 5001 | http://localhost:5001/metrics | Endpoint de métricas |

---

## Arquitetura de Observabilidade

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           DesafioComIA API                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │
│  │    Logs     │  │   Traces    │  │  Métricas   │                     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘                     │
│         │                │                │                             │
│         └────────────────┼────────────────┘                             │
│                          │                                              │
│                   ┌──────┴──────┐                                       │
│                   │ OpenTelemetry│                                       │
│                   │   SDK        │                                       │
│                   └──────┬──────┘                                       │
└──────────────────────────┼──────────────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           │               │               │
           ▼               ▼               ▼
    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │  Jaeger  │    │Prometheus│    │  Console │
    │  (OTLP)  │    │ (scrape) │    │  (dev)   │
    └────┬─────┘    └────┬─────┘    └──────────┘
         │               │
         │               │
         └───────┬───────┘
                 │
                 ▼
          ┌──────────┐
          │ Grafana  │
          │Dashboard │
          └──────────┘
```

---

## Componentes

### 1. OpenTelemetry SDK

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => { ... })
    .WithMetrics(metrics => { ... });

builder.Logging.AddOpenTelemetry(logging => { ... });
```

### 2. ActivitySource (Custom Tracing)

```csharp
// DiagnosticsConfig.cs
public static class DiagnosticsConfig
{
    public const string ServiceName = "DesafioComIA.Api";
    public const string ServiceVersion = "1.0.0";
    
    public static ActivitySource ActivitySource = new(ServiceName, ServiceVersion);
    public static ActivitySource CacheActivitySource = new($"{ServiceName}.Cache", ServiceVersion);
    public static ActivitySource DomainActivitySource = new($"{ServiceName}.Domain", ServiceVersion);
}
```

### 3. Meters (Custom Metrics)

```csharp
// ClienteMetrics.cs
public class ClienteMetrics
{
    private readonly Counter<long> _clientesCriados;
    private readonly Counter<long> _clientesAtualizados;
    private readonly Counter<long> _clientesRemovidos;
    private readonly Counter<long> _clientesBuscas;
    private readonly Histogram<double> _processamentoTempo;
}

// CacheMetrics.cs
public class CacheMetrics
{
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _cacheInvalidations;
    private readonly Histogram<double> _operationDuration;
}
```

---

## Configuração

### appsettings.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "DesafioComIA.Api",
    "ServiceVersion": "1.0.0",
    "EnableConsoleExporter": true,
    "Otlp": {
      "Endpoint": "http://localhost:4317",
      "Protocol": "Grpc"
    },
    "Tracing": {
      "Enabled": true,
      "SamplingProbability": 1.0
    },
    "Metrics": {
      "Enabled": true,
      "PrometheusEndpoint": "/metrics"
    },
    "Logging": {
      "Enabled": true,
      "IncludeFormattedMessage": true,
      "IncludeScopes": true
    }
  }
}
```

### Produção (Recomendações)

```json
{
  "OpenTelemetry": {
    "EnableConsoleExporter": false,
    "Tracing": {
      "SamplingProbability": 0.1
    }
  }
}
```

---

## Logs

### Formato

Logs estruturados em formato OpenTelemetry LogRecord:

```json
{
  "Timestamp": "2026-01-16T10:30:00.000Z",
  "Severity": "Information",
  "Body": "Cliente criado com sucesso",
  "Attributes": {
    "cliente.id": "123e4567-...",
    "correlation.id": "abc123",
    "operation": "CreateCliente"
  },
  "Resource": {
    "service.name": "DesafioComIA.Api",
    "service.version": "1.0.0"
  }
}
```

### Correlation ID

Todas as requisições possuem um Correlation ID propagado através de:
- Header HTTP `X-Correlation-ID`
- TraceId do OpenTelemetry
- Log scopes

```csharp
// CorrelationIdMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                       ?? Activity.Current?.TraceId.ToString()
                       ?? Guid.NewGuid().ToString();
    
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        await _next(context);
    }
}
```

---

## Traces

### Spans Automáticos

O OpenTelemetry instrumenta automaticamente:
- **HTTP Requests**: `http.server.request`
- **HTTP Client Calls**: `http.client.request`
- **Entity Framework**: `ef.query`, `ef.save`

### Spans Customizados

Criados manualmente nos handlers:

| Span | Descrição | Atributos |
|------|-----------|-----------|
| `CreateCliente` | Criação de cliente | `cliente.id`, `cliente.nome`, `cliente.cpf` (mascarado) |
| `UpdateCliente` | Atualização completa | `cliente.id`, campos atualizados |
| `PatchCliente` | Atualização parcial | `cliente.id`, campos alterados |
| `DeleteCliente` | Remoção de cliente | `cliente.id` |
| `ListClientes` | Listagem paginada | `page`, `pageSize`, `totalCount` |
| `SearchClientes` | Busca com filtros | `filtros.*`, `totalCount` |
| `GetClienteById` | Busca por ID | `cliente.id` |

### Exemplo de Trace

```
[Trace ID: abc123...]
├── POST /api/clientes (http.server.request)
│   ├── CreateCliente (custom span)
│   │   ├── Evento: ValidandoValueObjects
│   │   ├── Evento: VerificandoDuplicidade
│   │   ├── ef.query (verificação CPF)
│   │   ├── ef.query (verificação Email)
│   │   ├── Evento: CriandoCliente
│   │   ├── ef.save (insert)
│   │   └── Evento: ClienteCriado
│   └── CacheInvalidation (cache span)
└── Response: 201 Created
```

---

## Métricas

### Métricas Automáticas

| Métrica | Tipo | Descrição |
|---------|------|-----------|
| `http_server_request_duration_seconds` | Histogram | Latência de requisições HTTP |
| `http_server_active_requests` | Gauge | Requisições ativas |
| `process_cpu_seconds_total` | Counter | Tempo de CPU consumido |
| `process_memory_bytes` | Gauge | Memória utilizada |

### Métricas de Negócio (ClienteMetrics)

| Métrica | Tipo | Labels | Descrição |
|---------|------|--------|-----------|
| `clientes_criados_total` | Counter | - | Total de clientes criados |
| `clientes_atualizados_total` | Counter | - | Total de atualizações |
| `clientes_removidos_total` | Counter | - | Total de remoções |
| `clientes_buscas_total` | Counter | `operacao` | Total de buscas |
| `clientes_processamento_tempo_seconds` | Histogram | `operacao` | Tempo de processamento |

### Métricas de Cache (CacheMetrics)

| Métrica | Tipo | Labels | Descrição |
|---------|------|--------|-----------|
| `cache_hits_total` | Counter | `operacao` | Total de cache hits |
| `cache_misses_total` | Counter | `operacao` | Total de cache misses |
| `cache_invalidations_total` | Counter | `operacao` | Total de invalidações |
| `cache_operation_duration_seconds` | Histogram | `operacao` | Duração de operações |

---

## Ferramentas de Visualização

### Jaeger UI (http://localhost:16686)

**Como usar:**
1. Acesse http://localhost:16686
2. Selecione o serviço `DesafioComIA.Api`
3. Defina o intervalo de tempo
4. Clique em "Find Traces"

**Recursos:**
- Visualização de traces distribuídos
- Análise de latência por span
- Comparação de traces
- Busca por tags e atributos

### Prometheus (http://localhost:9090)

**Como usar:**
1. Acesse http://localhost:9090
2. Use o campo de query para consultar métricas
3. Visualize em tabela ou gráfico

### Grafana (http://localhost:3000)

**Credenciais:** admin / admin

**Dashboards Disponíveis:**
- **DesafioComIA API Overview**: Visão geral da API

**Recursos:**
- Dashboards personalizáveis
- Alertas
- Integração com Prometheus e Jaeger

---

## Queries Úteis

### Prometheus

#### Taxa de Requisições por Endpoint
```promql
sum(rate(http_server_request_duration_seconds_count[5m])) by (http_route)
```

#### Latência P95
```promql
histogram_quantile(0.95, 
  sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le, http_route)
)
```

#### Taxa de Erros
```promql
sum(rate(http_server_request_duration_seconds_count{http_status_code=~"5.."}[5m])) 
  / 
sum(rate(http_server_request_duration_seconds_count[5m])) * 100
```

#### Cache Hit Rate
```promql
rate(cache_hits_total[5m]) / (rate(cache_hits_total[5m]) + rate(cache_misses_total[5m])) * 100
```

#### Clientes Criados por Minuto
```promql
rate(clientes_criados_total[1m]) * 60
```

### Jaeger

#### Buscar por Correlation ID
```
correlationId=abc123
```

#### Buscar por Cliente ID
```
cliente.id=123e4567-...
```

#### Buscar por Operação
```
operation=CreateCliente
```

---

## Mascaramento de Dados Sensíveis

### Dados Mascarados

| Dado | Original | Mascarado |
|------|----------|-----------|
| CPF | `123.456.789-00` | `***.456.789-**` |
| Email | `user@example.com` | `u***@example.com` |

### Implementação

```csharp
// SensitiveDataProcessor.cs
public static class SensitiveDataProcessor
{
    public static string MaskCpf(string cpf)
    {
        if (string.IsNullOrEmpty(cpf) || cpf.Length < 11)
            return "***";
        
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        return $"***.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-**";
    }
    
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "***";
        
        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***";
        
        var local = parts[0];
        var domain = parts[1];
        
        return $"{local[0]}***@{domain}";
    }
}
```

### Uso nos Handlers

```csharp
// ActivityExtensions.cs
public static void SetClienteTag(this Activity? activity, string nome, string cpf, string email)
{
    activity?.SetTag("cliente.nome", nome);
    activity?.SetTag("cliente.cpf", SensitiveDataProcessor.MaskCpf(cpf));
    activity?.SetTag("cliente.email", SensitiveDataProcessor.MaskEmail(email));
}
```

---

## Subindo a Stack

### Docker Compose

```bash
# Subir todos os serviços
docker-compose up -d

# Verificar status
docker-compose ps

# Ver logs
docker-compose logs -f jaeger
docker-compose logs -f prometheus
docker-compose logs -f grafana
```

### Verificar Health

```bash
# Jaeger
curl http://localhost:16686/

# Prometheus
curl http://localhost:9090/-/healthy

# Grafana
curl http://localhost:3000/api/health

# API Metrics
curl http://localhost:5001/metrics
```

---

## Troubleshooting

### Traces não aparecem no Jaeger

1. Verificar se Jaeger está rodando: `docker-compose ps jaeger`
2. Verificar endpoint OTLP: `OpenTelemetry.Otlp.Endpoint`
3. Verificar se tracing está habilitado: `OpenTelemetry.Tracing.Enabled`
4. Verificar sampling: `SamplingProbability` > 0

### Métricas não aparecem no Prometheus

1. Verificar se o target está UP: http://localhost:9090/targets
2. Verificar se o endpoint está acessível: `curl http://localhost:5001/metrics`
3. Verificar configuração em `monitoring/prometheus.yml`

### Grafana não mostra dados

1. Verificar datasources configurados
2. Verificar se Prometheus está coletando métricas
3. Verificar intervalo de tempo no dashboard

### Console exporter não mostra nada

1. Verificar `EnableConsoleExporter: true`
2. Verificar nível de log não está suprimindo
3. Executar em modo Debug para ver output detalhado
