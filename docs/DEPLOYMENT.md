# Guia de Deploy

Este documento descreve as estratégias e configurações para deploy da aplicação DesafioComIA API em diferentes ambientes.

## Sumário

- [Visão Geral](#visão-geral)
- [Estratégias de Deploy](#estratégias-de-deploy)
- [Configuração de Ambiente](#configuração-de-ambiente)
- [Deploy com Docker](#deploy-com-docker)
- [Deploy em Kubernetes](#deploy-em-kubernetes)
- [Configuração de Infraestrutura](#configuração-de-infraestrutura)
- [Health Checks](#health-checks)
- [Monitoramento em Produção](#monitoramento-em-produção)
- [Checklist de Deploy](#checklist-de-deploy)

---

## Visão Geral

### Requisitos de Infraestrutura

| Componente | Requisito | Observação |
|------------|-----------|------------|
| Runtime | .NET 9 | Ou container com runtime |
| PostgreSQL | 12+ | Recomendado: 15 |
| Redis | 6+ | Opcional, mas recomendado |
| Memória | 512MB+ | Mínimo para API |
| CPU | 1 core | Escalar conforme carga |

### Componentes do Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                      Load Balancer                          │
└─────────────────────────┬───────────────────────────────────┘
                          │
         ┌────────────────┼────────────────┐
         │                │                │
         ▼                ▼                ▼
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│   API #1    │  │   API #2    │  │   API #3    │
└──────┬──────┘  └──────┬──────┘  └──────┬──────┘
       │                │                │
       └────────────────┼────────────────┘
                        │
         ┌──────────────┴──────────────┐
         │                             │
         ▼                             ▼
┌─────────────────┐           ┌─────────────────┐
│   PostgreSQL    │           │     Redis       │
│   (Primary)     │           │   (Cluster)     │
└─────────────────┘           └─────────────────┘
```

---

## Estratégias de Deploy

### 1. Single Instance (Desenvolvimento/Staging)

- Uma instância da API
- PostgreSQL e Redis em containers
- Ideal para ambientes de teste

### 2. Multi-Instance (Produção)

- Múltiplas instâncias da API atrás de load balancer
- PostgreSQL com réplicas
- Redis em cluster
- Observabilidade centralizada

### 3. Kubernetes (Produção Escalável)

- Deployment com múltiplas réplicas
- Horizontal Pod Autoscaler
- Ingress para roteamento
- ConfigMaps e Secrets para configuração

---

## Configuração de Ambiente

### Variáveis de Ambiente Obrigatórias

```bash
# Connection Strings
ConnectionStrings__DefaultConnection="Host=db-host;Port=5432;Database=DesafioComIA;User Id=app_user;Password=secure_password"
ConnectionStrings__Redis="redis-host:6379,abortConnect=false,password=redis_password"

# Observabilidade
OpenTelemetry__Otlp__Endpoint="http://otel-collector:4317"
OpenTelemetry__ServiceName="DesafioComIA.Api"

# Cache
Cache__Enabled=true
```

### Variáveis de Ambiente Recomendadas (Produção)

```bash
# Desabilitar console exporter
OpenTelemetry__EnableConsoleExporter=false

# Reduzir sampling
OpenTelemetry__Tracing__SamplingProbability=0.1

# Aumentar TTL de cache
Cache__DefaultTTLMinutes=10
Cache__ListClientesTTLMinutes=10
Cache__GetClienteByIdTTLMinutes=30

# Logging
Logging__LogLevel__Default=Warning
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

### appsettings.Production.json

```json
{
  "Cache": {
    "Enabled": true,
    "DefaultTTLMinutes": 10,
    "ListClientesTTLMinutes": 10,
    "GetClienteByIdTTLMinutes": 30,
    "SearchClientesTTLMinutes": 5
  },
  "OpenTelemetry": {
    "EnableConsoleExporter": false,
    "Tracing": {
      "SamplingProbability": 0.1
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## Deploy com Docker

### Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/DesafioComIA.Api/DesafioComIA.Api.csproj", "DesafioComIA.Api/"]
COPY ["src/DesafioComIA.Application/DesafioComIA.Application.csproj", "DesafioComIA.Application/"]
COPY ["src/DesafioComIA.Domain/DesafioComIA.Domain.csproj", "DesafioComIA.Domain/"]
COPY ["src/DesafioComIA.Infrastructure/DesafioComIA.Infrastructure.csproj", "DesafioComIA.Infrastructure/"]
RUN dotnet restore "DesafioComIA.Api/DesafioComIA.Api.csproj"

# Copy everything and build
COPY src/ .
RUN dotnet build "DesafioComIA.Api/DesafioComIA.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DesafioComIA.Api/DesafioComIA.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DesafioComIA.Api.dll"]
```

### Build e Run

```bash
# Build
docker build -t desafiocomia-api:latest .

# Run
docker run -d \
  --name desafiocomia-api \
  -p 5001:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;..." \
  -e ConnectionStrings__Redis="host.docker.internal:6379" \
  desafiocomia-api:latest
```

### Docker Compose (Produção)

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=DesafioComIA;User Id=app_user;Password=${DB_PASSWORD}
      - ConnectionStrings__Redis=redis:6379,abortConnect=false
      - OpenTelemetry__Otlp__Endpoint=http://otel-collector:4317
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    deploy:
      replicas: 3
      resources:
        limits:
          cpus: '1'
          memory: 512M
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: DesafioComIA
      POSTGRES_USER: app_user
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app_user -d DesafioComIA"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis-data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD}", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

volumes:
  postgres-data:
  redis-data:
```

---

## Deploy em Kubernetes

### Namespace

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: desafiocomia
```

### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: desafiocomia-config
  namespace: desafiocomia
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  Cache__Enabled: "true"
  Cache__DefaultTTLMinutes: "10"
  OpenTelemetry__EnableConsoleExporter: "false"
  OpenTelemetry__Tracing__SamplingProbability: "0.1"
```

### Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: desafiocomia-secrets
  namespace: desafiocomia
type: Opaque
stringData:
  ConnectionStrings__DefaultConnection: "Host=postgres-svc;Port=5432;Database=DesafioComIA;User Id=app_user;Password=secure_password"
  ConnectionStrings__Redis: "redis-svc:6379,abortConnect=false,password=redis_password"
```

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: desafiocomia-api
  namespace: desafiocomia
spec:
  replicas: 3
  selector:
    matchLabels:
      app: desafiocomia-api
  template:
    metadata:
      labels:
        app: desafiocomia-api
    spec:
      containers:
      - name: api
        image: desafiocomia-api:latest
        ports:
        - containerPort: 8080
        envFrom:
        - configMapRef:
            name: desafiocomia-config
        - secretRef:
            name: desafiocomia-secrets
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
```

### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: desafiocomia-api-svc
  namespace: desafiocomia
spec:
  selector:
    app: desafiocomia-api
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
```

### Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: desafiocomia-ingress
  namespace: desafiocomia
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
  - host: api.desafiocomia.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: desafiocomia-api-svc
            port:
              number: 80
```

### HorizontalPodAutoscaler

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: desafiocomia-api-hpa
  namespace: desafiocomia
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: desafiocomia-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

---

## Configuração de Infraestrutura

### PostgreSQL em Produção

**Recomendações:**
- Use managed database (AWS RDS, Azure Database, GCP Cloud SQL)
- Configure réplicas de leitura para alta disponibilidade
- Habilite backups automáticos
- Configure connection pooling (PgBouncer)

**Parâmetros Recomendados:**
```
max_connections = 200
shared_buffers = 256MB
effective_cache_size = 768MB
work_mem = 4MB
maintenance_work_mem = 64MB
```

### Redis em Produção

**Recomendações:**
- Use managed Redis (AWS ElastiCache, Azure Cache for Redis)
- Configure cluster para alta disponibilidade
- Habilite persistência (AOF)
- Configure maxmemory-policy para eviction

**Parâmetros Recomendados:**
```
maxmemory 256mb
maxmemory-policy allkeys-lru
appendonly yes
```

### OpenTelemetry Collector em Produção

Use um OpenTelemetry Collector centralizado:

```yaml
# otel-collector-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317

processors:
  batch:
    timeout: 10s

exporters:
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true
  prometheus:
    endpoint: "0.0.0.0:8889"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

---

## Health Checks

### Endpoints Disponíveis

| Endpoint | Propósito | Uso |
|----------|-----------|-----|
| `/health` | Health check geral | Load balancer |
| `/health/live` | Liveness probe | Kubernetes |
| `/health/ready` | Readiness probe | Kubernetes |

### Configuração

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql")
    .AddRedis(redisConnectionString, name: "redis");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Apenas retorna OK se a aplicação está rodando
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

---

## Monitoramento em Produção

### Alertas Recomendados

| Alerta | Condição | Severidade |
|--------|----------|------------|
| High Error Rate | `error_rate > 5%` por 5min | Critical |
| High Latency | `p95 > 2s` por 5min | Warning |
| Low Cache Hit Rate | `hit_rate < 50%` por 15min | Warning |
| Database Connection Failed | `health_check = unhealthy` | Critical |
| High Memory Usage | `memory > 80%` por 10min | Warning |
| High CPU Usage | `cpu > 80%` por 10min | Warning |

### Queries de Alerta (Prometheus)

```promql
# Error Rate > 5%
sum(rate(http_server_request_duration_seconds_count{http_status_code=~"5.."}[5m])) 
/ sum(rate(http_server_request_duration_seconds_count[5m])) > 0.05

# P95 Latency > 2s
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le)) > 2

# Cache Hit Rate < 50%
rate(cache_hits_total[15m]) / (rate(cache_hits_total[15m]) + rate(cache_misses_total[15m])) < 0.5
```

---

## Checklist de Deploy

### Antes do Deploy

- [ ] Testes passando (unitários e integração)
- [ ] Build de produção sem warnings
- [ ] Migrations aplicadas ou preparadas
- [ ] Variáveis de ambiente configuradas
- [ ] Secrets seguros (não expostos em logs/config)
- [ ] Backup do banco de dados (se atualização)
- [ ] Plano de rollback definido

### Durante o Deploy

- [ ] Deploy gradual (canary ou rolling)
- [ ] Monitorar health checks
- [ ] Monitorar métricas de erro
- [ ] Monitorar latência
- [ ] Verificar logs

### Após o Deploy

- [ ] Validar endpoints críticos
- [ ] Verificar conexão com banco
- [ ] Verificar conexão com Redis
- [ ] Verificar traces no Jaeger
- [ ] Verificar métricas no Grafana
- [ ] Comunicar equipe sobre conclusão

### Rollback

```bash
# Docker
docker-compose down
docker-compose up -d --scale api=0
docker-compose up -d api  # com imagem anterior

# Kubernetes
kubectl rollout undo deployment/desafiocomia-api -n desafiocomia
```
