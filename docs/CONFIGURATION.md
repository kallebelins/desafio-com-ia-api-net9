# Guia de Configuração

Este documento descreve todas as configurações disponíveis na aplicação DesafioComIA API.

## Sumário

- [Connection Strings](#connection-strings)
- [Configuração de Cache](#configuração-de-cache)
- [Configuração de Observabilidade](#configuração-de-observabilidade)
- [Configuração de Logging](#configuração-de-logging)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Configurações por Ambiente](#configurações-por-ambiente)

---

## Connection Strings

### PostgreSQL

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Pooling=true;Database=DesafioComIA;User Id=postgres;Password=postgres;"
  }
}
```

**Parâmetros:**
| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `Host` | Endereço do servidor PostgreSQL | `localhost` |
| `Port` | Porta do PostgreSQL | `5432` |
| `Database` | Nome do banco de dados | `DesafioComIA` |
| `User Id` | Usuário do banco | `postgres` |
| `Password` | Senha do usuário | `postgres` |
| `Pooling` | Habilita connection pooling | `true` |

### Redis

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

**Parâmetros:**
| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| Endpoint | Endereço do servidor Redis | `localhost:6379` |
| `abortConnect` | Se `false`, não falha se não conseguir conectar | `false` |

---

## Configuração de Cache

O sistema utiliza **HybridCache** do .NET 9 com suporte a dois níveis de cache:
- **L1 (Memória)**: Cache local em memória para acesso rápido
- **L2 (Redis)**: Cache distribuído para consistência entre instâncias

```json
{
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

**Parâmetros:**
| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `Enabled` | Habilita/desabilita o cache globalmente | `true` |
| `DefaultTTLMinutes` | TTL padrão para operações não especificadas | `5` |
| `ListClientesTTLMinutes` | TTL para listagem de clientes | `5` |
| `GetClienteByIdTTLMinutes` | TTL para busca de cliente por ID | `10` |
| `SearchClientesTTLMinutes` | TTL para busca com filtros | `3` |
| `LocalCacheTTLMinutes` | TTL do cache L1 (memória local) | `1` |
| `MaximumPayloadBytes` | Tamanho máximo do valor em bytes | `1048576` (1MB) |
| `MaximumKeyLength` | Tamanho máximo da chave | `1024` |
| `KeyPrefix` | Prefixo para todas as chaves de cache | `desafiocomia:` |

### Comportamento do Cache

- **Cache Habilitado**: Consultas são cacheadas com TTL configurado
- **Cache Desabilitado**: Todas as consultas vão diretamente ao banco
- **Redis Indisponível**: Sistema continua funcionando apenas com cache L1 (memória)

---

## Configuração de Observabilidade

O sistema utiliza **OpenTelemetry** para logs, traces e métricas.

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

### Parâmetros Gerais

| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `ServiceName` | Nome do serviço nos traces | `DesafioComIA.Api` |
| `ServiceVersion` | Versão do serviço | `1.0.0` |
| `EnableConsoleExporter` | Exporta telemetria para console | `true` |

### Configuração OTLP

| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `Otlp.Endpoint` | Endpoint do coletor OTLP (Jaeger) | `http://localhost:4317` |
| `Otlp.Protocol` | Protocolo de comunicação | `Grpc` |

### Configuração de Tracing

| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `Tracing.Enabled` | Habilita tracing | `true` |
| `Tracing.SamplingProbability` | Taxa de amostragem (1.0 = 100%) | `1.0` |

### Configuração de Métricas

| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `Metrics.Enabled` | Habilita métricas | `true` |
| `Metrics.PrometheusEndpoint` | Endpoint para scraping do Prometheus | `/metrics` |

### Configuração de Logging

| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `Logging.Enabled` | Habilita logging via OpenTelemetry | `true` |
| `Logging.IncludeFormattedMessage` | Inclui mensagem formatada | `true` |
| `Logging.IncludeScopes` | Inclui escopos de logging | `true` |

---

## Configuração de Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "OpenTelemetry": "Warning"
    }
  }
}
```

**Níveis de Log Disponíveis:**
- `Trace` - Informações detalhadas de diagnóstico
- `Debug` - Informações para debugging
- `Information` - Informações gerais de fluxo
- `Warning` - Alertas que não impedem funcionamento
- `Error` - Erros que afetam operações específicas
- `Critical` - Erros que afetam toda a aplicação
- `None` - Desabilita logging

---

## Variáveis de Ambiente

A aplicação suporta configuração via variáveis de ambiente, seguindo o padrão do ASP.NET Core:

### Connection Strings
```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;..."
ConnectionStrings__Redis="localhost:6379,abortConnect=false"
```

### Cache
```bash
Cache__Enabled=true
Cache__DefaultTTLMinutes=5
Cache__ListClientesTTLMinutes=5
Cache__GetClienteByIdTTLMinutes=10
Cache__SearchClientesTTLMinutes=3
```

### OpenTelemetry
```bash
OpenTelemetry__ServiceName=DesafioComIA.Api
OpenTelemetry__Otlp__Endpoint=http://localhost:4317
OpenTelemetry__Tracing__Enabled=true
OpenTelemetry__Metrics__Enabled=true
```

### Logging
```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

---

## Configurações por Ambiente

### Development (`appsettings.Development.json`)

```json
{
  "OpenTelemetry": {
    "EnableConsoleExporter": true,
    "Tracing": {
      "SamplingProbability": 1.0
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### Production (`appsettings.Production.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db-server;Port=5432;...",
    "Redis": "redis-server:6379,abortConnect=false"
  },
  "Cache": {
    "Enabled": true,
    "DefaultTTLMinutes": 10
  },
  "OpenTelemetry": {
    "EnableConsoleExporter": false,
    "Tracing": {
      "SamplingProbability": 0.1
    },
    "Otlp": {
      "Endpoint": "http://otel-collector:4317"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## Dicas de Configuração

### Para Performance em Produção

1. **Cache TTL**: Aumente o TTL para reduzir carga no banco
2. **Sampling**: Reduza `SamplingProbability` para 0.1 (10%) em produção
3. **Console Exporter**: Desabilite em produção
4. **Log Level**: Use `Warning` ou superior em produção

### Para Debugging

1. **Console Exporter**: Habilite para ver traces no console
2. **Sampling**: Mantenha em 1.0 (100%)
3. **Log Level**: Use `Debug` ou `Trace`
4. **Cache**: Considere desabilitar temporariamente para isolar problemas

### Segurança

1. **Nunca** commite `appsettings.Development.json` ou `appsettings.Production.json` com senhas reais
2. Use **variáveis de ambiente** ou **secrets managers** para dados sensíveis
3. O arquivo `.gitignore` já ignora esses arquivos por padrão
