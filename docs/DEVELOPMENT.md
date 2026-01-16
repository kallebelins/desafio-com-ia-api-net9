# Guia de Desenvolvimento

Este documento descreve como configurar e utilizar o ambiente de desenvolvimento para a aplicação DesafioComIA API.

## Sumário

- [Pré-requisitos](#pré-requisitos)
- [Configuração do Ambiente](#configuração-do-ambiente)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Executando a Aplicação](#executando-a-aplicação)
- [Executando Testes](#executando-testes)
- [Convenções de Código](#convenções-de-código)
- [Fluxo de Desenvolvimento](#fluxo-de-desenvolvimento)
- [Debugging](#debugging)

---

## Pré-requisitos

### Software Necessário

| Software | Versão Mínima | Download |
|----------|---------------|----------|
| .NET SDK | 9.0 | https://dotnet.microsoft.com/download/dotnet/9.0 |
| Docker | 20.10+ | https://docs.docker.com/get-docker/ |
| Docker Compose | 2.0+ | Incluído no Docker Desktop |
| Git | 2.30+ | https://git-scm.com/downloads |

### IDEs Recomendadas

- **Visual Studio 2022** (17.8+) - Melhor integração com .NET
- **VS Code** com extensões C# Dev Kit - Leve e multiplataforma
- **JetBrains Rider** - IDE completa multiplataforma

### Extensões VS Code Recomendadas

```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
    "ms-dotnettools.csharp",
    "ms-azuretools.vscode-docker",
    "humao.rest-client",
    "eamodio.gitlens"
  ]
}
```

---

## Configuração do Ambiente

### 1. Clonar o Repositório

```bash
git clone <repository-url>
cd desafio-com-ia-api-net9
```

### 2. Restaurar Dependências

```bash
dotnet restore
```

### 3. Subir Infraestrutura com Docker Compose

```bash
# Subir todos os serviços (PostgreSQL, Redis, Jaeger, Prometheus, Grafana)
docker-compose up -d

# Verificar se todos os serviços estão rodando
docker-compose ps
```

**Serviços disponíveis:**

| Serviço | Porta | Descrição |
|---------|-------|-----------|
| PostgreSQL | 5432 | Banco de dados |
| Redis | 6379 | Cache distribuído |
| Jaeger | 16686 | UI de traces |
| Prometheus | 9090 | Métricas |
| Grafana | 3000 | Dashboards |

### 4. Aplicar Migrations

```bash
dotnet ef database update \
  --project src/DesafioComIA.Infrastructure \
  --startup-project src/DesafioComIA.Api
```

### 5. Executar a Aplicação

```bash
dotnet run --project src/DesafioComIA.Api
```

### 6. Verificar se está funcionando

- **Swagger**: http://localhost:5001/swagger
- **Health Check**: http://localhost:5001/health
- **Métricas**: http://localhost:5001/metrics

---

## Estrutura do Projeto

```
desafio-com-ia-api-net9/
├── src/
│   ├── DesafioComIA.Api/                 # Camada de Apresentação
│   │   ├── Controllers/                  # Endpoints REST
│   │   ├── Middleware/                   # Middlewares customizados
│   │   ├── Configuration/                # Classes de configuração
│   │   └── Program.cs                    # Entry point e DI
│   │
│   ├── DesafioComIA.Application/         # Camada de Aplicação
│   │   ├── Commands/                     # CQRS Commands
│   │   │   └── Cliente/
│   │   │       ├── CreateClienteCommand.cs
│   │   │       ├── CreateClienteCommandHandler.cs
│   │   │       └── CreateClienteCommandValidator.cs
│   │   ├── Queries/                      # CQRS Queries
│   │   │   └── Cliente/
│   │   │       ├── GetClientesQuery.cs
│   │   │       ├── GetClientesQueryHandler.cs
│   │   │       └── GetClientesQueryValidator.cs
│   │   ├── DTOs/                         # Data Transfer Objects
│   │   ├── Mapping/                      # AutoMapper Profiles
│   │   └── Telemetry/                    # Métricas e tracing
│   │
│   ├── DesafioComIA.Domain/              # Camada de Domínio
│   │   ├── Entities/                     # Entidades de domínio
│   │   └── Exceptions/                   # Exceções de domínio
│   │
│   └── DesafioComIA.Infrastructure/      # Camada de Infraestrutura
│       ├── Data/                         # DbContext e configurações EF
│       ├── Caching/                      # Implementação de cache
│       ├── Configuration/                # Classes de configuração
│       └── Telemetry/                    # Métricas de infraestrutura
│
├── tests/
│   └── DesafioComIA.Tests/               # Testes de integração
│
├── monitoring/                           # Configurações de observabilidade
│   ├── prometheus.yml
│   └── grafana/
│       └── provisioning/
│
├── docs/                                 # Documentação
├── tasks/                                # Documentação de tarefas (MDPE)
├── docker-compose.yml                    # Infraestrutura local
└── DesafioComIA.sln                      # Solution file
```

### Responsabilidades por Camada

#### API (Apresentação)
- Controllers REST
- Middlewares (Exception, Correlation ID)
- Configuração de DI
- Configuração de OpenTelemetry

#### Application
- Commands e Queries (CQRS)
- Handlers
- Validators (FluentValidation)
- DTOs
- Mapeamentos (AutoMapper)
- Métricas de negócio

#### Domain
- Entidades
- Value Objects
- Exceções de domínio
- Regras de negócio

#### Infrastructure
- DbContext (EF Core)
- Configurações de entidades
- Implementação de cache
- Repositórios (via Mvp24Hours)

---

## Executando a Aplicação

### Modo Development

```bash
# Via CLI
dotnet run --project src/DesafioComIA.Api

# Via watch (hot reload)
dotnet watch run --project src/DesafioComIA.Api
```

### Variáveis de Ambiente Úteis

```bash
# Desabilitar cache para debugging
export Cache__Enabled=false

# Aumentar verbosidade de logs
export Logging__LogLevel__Default=Debug

# Usar outra porta
export ASPNETCORE_URLS=http://localhost:5002
```

---

## Executando Testes

### Todos os Testes

```bash
dotnet test
```

### Testes com Cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Testes Específicos

```bash
# Por nome
dotnet test --filter "FullyQualifiedName~CreateCliente"

# Por categoria
dotnet test --filter "Category=Integration"
```

### Gerar Relatório de Cobertura

```bash
# Instalar ferramenta de relatório
dotnet tool install -g dotnet-reportgenerator-globaltool

# Gerar relatório HTML
reportgenerator \
  -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
```

---

## Convenções de Código

### Nomenclatura

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Classes | PascalCase | `ClienteController` |
| Interfaces | IPascalCase | `ICacheService` |
| Métodos | PascalCase | `CreateCliente` |
| Parâmetros | camelCase | `clienteId` |
| Variáveis locais | camelCase | `cliente` |
| Constantes | PascalCase | `ServiceName` |
| Campos privados | _camelCase | `_repository` |

### Estrutura de Commands/Queries

```csharp
// Command
public record CreateClienteCommand(
    string Nome,
    string Cpf,
    string Email
) : IMediatorCommand<ClienteDto>;

// Validator
public class CreateClienteCommandValidator : AbstractValidator<CreateClienteCommand>
{
    public CreateClienteCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");
    }
}

// Handler
public class CreateClienteCommandHandler : IMediatorCommandHandler<CreateClienteCommand, ClienteDto>
{
    public async Task<ClienteDto> Handle(
        CreateClienteCommand command, 
        CancellationToken cancellationToken)
    {
        // implementação
    }
}
```

### Organização de Arquivos

- Um arquivo por classe/interface
- Nome do arquivo = nome da classe
- Agrupar por feature (não por tipo)

### Comentários XML

```csharp
/// <summary>
/// Cria um novo cliente no sistema.
/// </summary>
/// <param name="dto">Dados do cliente a ser criado</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>Cliente criado com ID gerado</returns>
/// <response code="201">Cliente criado com sucesso</response>
/// <response code="400">Dados inválidos</response>
/// <response code="409">CPF ou Email já cadastrado</response>
[HttpPost]
[ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public async Task<IActionResult> Create([FromBody] CreateClienteDto dto, CancellationToken ct)
```

---

## Fluxo de Desenvolvimento

### 1. Criar Branch

```bash
git checkout -b feature/nome-da-feature
```

### 2. Implementar Feature

Seguir padrão CQRS:
1. Criar Command/Query
2. Criar Validator
3. Criar Handler
4. Criar/Atualizar DTO
5. Adicionar endpoint no Controller
6. Criar testes

### 3. Testar Localmente

```bash
# Executar testes
dotnet test

# Testar manualmente via Swagger
dotnet run --project src/DesafioComIA.Api
```

### 4. Commit

```bash
git add .
git commit -m "feat: descrição da feature"
```

### 5. Push e PR

```bash
git push origin feature/nome-da-feature
```

---

## Debugging

### Visual Studio

1. Abrir solution `DesafioComIA.sln`
2. Definir `DesafioComIA.Api` como projeto de inicialização
3. F5 para iniciar debugging

### VS Code

1. Abrir pasta do projeto
2. Ir em "Run and Debug" (Ctrl+Shift+D)
3. Selecionar ".NET Core Launch (web)"
4. F5 para iniciar

### Breakpoints Úteis

- `ExceptionHandlingMiddleware.InvokeAsync` - Captura de exceções
- `CreateClienteCommandHandler.Handle` - Criação de cliente
- `HybridCacheService.GetOrCreateAsync` - Operações de cache

### Logs de Debug

```bash
# Aumentar verbosidade
export Logging__LogLevel__Default=Debug
export Logging__LogLevel__Microsoft.EntityFrameworkCore=Information

# Executar
dotnet run --project src/DesafioComIA.Api
```

### Inspecionar Cache (Redis)

```bash
# Conectar ao Redis CLI
docker exec -it desafio_redis redis-cli

# Listar chaves
KEYS desafiocomia:*

# Ver valor de uma chave
GET desafiocomia:clientes:id:123e4567-...

# Limpar tudo
FLUSHALL
```

### Inspecionar Banco (PostgreSQL)

```bash
# Conectar ao psql
docker exec -it desafio_postgres psql -U postgres -d DesafioComIA

# Listar clientes
SELECT * FROM "Clientes";

# Ver estrutura da tabela
\d "Clientes"
```

---

## Comandos Úteis

### Docker Compose

```bash
# Subir serviços
docker-compose up -d

# Parar serviços
docker-compose down

# Ver logs
docker-compose logs -f [serviço]

# Reiniciar serviço específico
docker-compose restart postgres

# Limpar volumes (APAGA DADOS!)
docker-compose down -v
```

### .NET CLI

```bash
# Build
dotnet build

# Limpar
dotnet clean

# Restaurar
dotnet restore

# Adicionar pacote
dotnet add src/DesafioComIA.Api package NomeDoPacote

# Adicionar migration
dotnet ef migrations add NomeDaMigration \
  --project src/DesafioComIA.Infrastructure \
  --startup-project src/DesafioComIA.Api

# Aplicar migrations
dotnet ef database update \
  --project src/DesafioComIA.Infrastructure \
  --startup-project src/DesafioComIA.Api
```

### Git

```bash
# Ver status
git status

# Ver branches
git branch -a

# Atualizar main
git checkout main && git pull

# Rebase com main
git rebase main
```
