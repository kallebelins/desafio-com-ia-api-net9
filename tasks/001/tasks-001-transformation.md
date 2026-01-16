# Tasks Transformation - API de Cliente com CQRS

## 📋 Visão Geral

Este documento contém todas as microtarefas detalhadas para implementação do backlog de API de Cliente utilizando:
- **Arquitetura**: CQRS (Command Query Responsibility Segregation)
- **Framework**: Mvp24Hours .NET 9
- **Banco de Dados**: PostgreSQL
- **Padrões**: Repository, Unit of Work, Mediator

---

## 🌊 Wave 1: Configuração da Arquitetura Base

### Objetivo
Configurar a estrutura base do projeto, dependências e infraestrutura necessária para suportar CQRS com PostgreSQL.

### Microtarefas

#### W1.1: Criar Estrutura de Projeto
- [x] Criar projeto WebAPI .NET 9: `dotnet new webapi -n DesafioComIA.Api`
- [x] Criar estrutura de pastas:
  - `src/DesafioComIA.Api/` (API Layer)
  - `src/DesafioComIA.Application/` (Application Layer - Commands/Queries)
  - `src/DesafioComIA.Domain/` (Domain Layer - Entities)
  - `src/DesafioComIA.Infrastructure/` (Infrastructure Layer - Data Access)
- [x] Configurar solution: `dotnet new sln -n DesafioComIA`
- [x] Adicionar projetos à solution
- [x] Configurar referências entre projetos:
  - API → Application, Infrastructure
  - Application → Infrastructure, Domain
  - Infrastructure → Domain
- [x] Adicione .gitignore

#### W1.2: Instalar Pacotes NuGet - Core
- [x] Instalar `Mvp24Hours.Core` (versão 9.*) em todos os projetos necessários
- [x] Instalar `Mvp24Hours.Application` (versão 9.*) no projeto Application
- [x] Instalar `Mvp24Hours.Infrastructure.Data.EFCore` (versão 9.*) no projeto Infrastructure
- [x] Instalar `Mvp24Hours.Infrastructure.Cqrs` (versão 9.*) no projeto Application
- [x] Instalar `Mvp24Hours.WebAPI` (versão 9.*) no projeto API

#### W1.3: Instalar Pacotes NuGet - PostgreSQL
- [x] Instalar `Npgsql.EntityFrameworkCore.PostgreSQL` (versão 9.*) no projeto Infrastructure
- [x] Instalar `Microsoft.EntityFrameworkCore.Design` (versão 9.*) no projeto Infrastructure
- [x] Instalar `Microsoft.EntityFrameworkCore.Tools` (versão 9.*) no projeto Infrastructure

#### W1.4: Instalar Pacotes NuGet - Validação e Mapeamento
- [x] Instalar `FluentValidation` (versão 12.*) no projeto Application
- [x] Instalar `FluentValidation.DependencyInjectionExtensions` (versão 12.*) no projeto API (substitui FluentValidation.AspNetCore que foi deprecado)
- [x] Instalar `AutoMapper` (versão 13.*) no projeto Application
- [x] ~~Instalar `AutoMapper.Extensions.Microsoft.DependencyInjection`~~ (Não necessário - integrado no AutoMapper 13.0+)

#### W1.5: Configurar appsettings.json
- [x] Criar arquivo `appsettings.json` no projeto API
- [x] Adicionar ConnectionString para PostgreSQL:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Port=5432;Pooling=true;Database=DesafioComIA;User Id=postgres;Password=postgres;"
    }
  }
  ```
- [x] Criar `appsettings.Development.json` com configurações de desenvolvimento
- [x] Criar `appsettings.Production.json` com configurações de produção

#### W1.6: Configurar Program.cs - Base
- [x] Configurar builder do WebApplication
- [x] Configurar logging básico
- [x] Configurar CORS (se necessário)
- [x] Configurar Swagger/OpenAPI usando Native OpenAPI do .NET 9 diretamente:
  ```csharp
  // Registrar serviços OpenAPI
  builder.Services.AddOpenApi("v1", options =>
  {
      options.AddDocumentTransformer((document, context, ct) =>
      {
          document.Info = new OpenApiInfo
          {
              Title = "DesafioComIA API",
              Version = "1.0.0",
              Description = "API para o Desafio com IA"
          };
          return System.Threading.Tasks.Task.CompletedTask;
      });
  });
  
  // No pipeline (após app.Build())
  app.MapOpenApi("/openapi/{documentName}.json");
  app.UseSwaggerUI(options =>
  {
      options.SwaggerEndpoint("/openapi/v1.json", "DesafioComIA API v1.0.0");
      options.RoutePrefix = "swagger";
  });
  ```
- [x] Nota: `AddMvp24HoursNativeOpenApi` do Mvp24Hours.WebAPI tinha um bug onde o `MapMvp24HoursNativeOpenApi` não registrava o middleware `UseSwaggerUI`, causando 404 no Swagger UI. Por isso foi substituído pela implementação direta.
- [x] Pacotes necessários: `Microsoft.AspNetCore.OpenApi` (nativo .NET 9) + `Swashbuckle.AspNetCore` (transitivo via Mvp24Hours.WebAPI) para UI

#### W1.7: Configurar Program.cs - PostgreSQL e DbContext
- [x] Criar classe `ApplicationDbContext` no projeto Infrastructure herdando de `Mvp24HoursContext`
- [x] Configurar DbContext no Program.cs:
  ```csharp
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
  ```
- [x] Registrar Mvp24Hours DbContext:
  ```csharp
  builder.Services.AddMvp24HoursDbContext<ApplicationDbContext>();
  ```
- [x] Registrar Repository Async:
  ```csharp
  builder.Services.AddMvp24HoursRepositoryAsync(options =>
  {
      options.MaxQtyByQueryPage = 100;
      options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
  });
  ```

#### W1.8: Configurar Program.cs - CQRS Mediator
- [x] Registrar Mediator:
  ```csharp
  builder.Services.AddMvpMediator(options =>
  {
      options.RegisterHandlersFromAssemblyContaining<Program>();
      options.RegisterLoggingBehavior = true;
      options.RegisterPerformanceBehavior = true;
      options.RegisterUnhandledExceptionBehavior = true;
      options.RegisterValidationBehavior = true;
      options.RegisterTransactionBehavior = true;
  });
  ```

#### W1.9: Configurar Program.cs - Validação
- [x] Registrar FluentValidation:
  ```csharp
  builder.Services.AddValidatorsFromAssemblyContaining<Program>();
  ```
  Nota: FluentValidation 12.x removeu métodos de auto-validação. A validação é feita automaticamente pelo ValidationBehavior do Mvp24Hours Mediator.

#### W1.10: Configurar Program.cs - AutoMapper
- [x] Registrar AutoMapper:
  ```csharp
  // Usando Mvp24Hours MapService (recomendado)
  var applicationAssembly = System.Reflection.Assembly.Load("DesafioComIA.Application");
  builder.Services.AddMvp24HoursMapService(
      typeof(Program).Assembly,
      applicationAssembly
  );
  ```
  Nota: Mvp24Hours fornece suporte integrado ao AutoMapper através do método `AddMvp24HoursMapService`.

#### W1.11: Configurar Health Checks
- [x] Adicionar Health Checks para PostgreSQL:
  ```csharp
  builder.Services.AddHealthChecks()
      .AddNpgSql(
          builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
          name: "postgresql",
          failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
  ```
- [x] Configurar endpoint de health check: `/health`

#### W1.12: Configurar Exception Handling
- [x] Criar middleware de tratamento de exceções
- [x] Configurar ProblemDetails para respostas de erro
- [x] Mapear exceções de validação para ProblemDetails
- [x] Mapear exceções de negócio para ProblemDetails

#### W1.13: Criar docker-compose.yml para ferramentas do projeto
- [x] Adicionar arquivo `docker-compose.yml` na raiz da solution contendo apenas os serviços utilizados pelo projeto:
  - `postgresql` com imagem oficial, volumes para persistência e variáveis de ambiente adequadas (POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD).
- [x] Exemplo básico de serviço PostgreSQL:
  ```yaml
  version: '3.8'
  services:
    postgres:
      image: postgres:15
      container_name: desafio_postgres
      restart: always
      environment:
        POSTGRES_DB: DesafioComIA
        POSTGRES_USER: postgres
        POSTGRES_PASSWORD: postgres
      ports:
        - "5432:5432"
      volumes:
        - ./data/postgres:/var/lib/postgresql/data
      healthcheck:
        test: ["CMD-SHELL", "pg_isready -U postgres"]
        interval: 10s
        timeout: 5s
        retries: 5
  ```
- [x] (Opcional) Documentar como subir e derrubar o ambiente:
  ```sh
  docker-compose up -d
  docker-compose down
  ```
- [x] Adicionar pasta `data/` ao `.gitignore` para ignorar dados do PostgreSQL
- [x] Atualizar README.md com instruções de uso do Docker Compose


---

## 🌊 Wave 2: Entidade e Contexto de Dados

### Objetivo
Criar a entidade Cliente, configurar o DbContext e preparar as migrations do banco de dados.

### Microtarefas

#### W2.1: Criar Entidade Cliente
- [x] Criar classe `Cliente` no projeto Domain
- [x] Herdar de `EntityBase<Guid>` do Mvp24Hours
- [x] Implementar propriedades:
  - `Id` (Guid) - herdado de EntityBase
  - `Nome` (string, obrigatório, 3-200 caracteres)
  - `Cpf` (ValueObject Cpf do Mvp24Hours, obrigatório, único)
  - `Email` (ValueObject Email do Mvp24Hours, obrigatório, único)
  - `CreatedAt` (DateTime) - herdado de EntityBase
  - `ModifiedAt` (DateTime?) - herdado de EntityBase
- [x] Adicionar construtor padrão
- [x] Adicionar construtor com parâmetros principais
- [x] Usar ValueObjects `Cpf` e `Email` já existentes do Mvp24Hours

#### W2.2: Configurar Entity no DbContext
- [x] Abrir `ApplicationDbContext` no projeto Infrastructure
- [x] Criar `DbSet<Cliente> Clientes { get; set; }`
- [x] Criar arquivo de configuração separado para a entidade `Cliente` usando Fluent API (`ClienteConfiguration.cs` em `Infrastructure/Data/Configurations`)
- [x] Registrar configuração usando `.ApplyConfiguration(new ClienteConfiguration())` em `OnModelCreating`
- [x] No arquivo de configuração, aplicar:
  - Nome da tabela: `"Clientes"`
  - Chave primária: `Id`
  - Índice único para `Cpf.Valor` (propriedade do ValueObject)
  - Índice único para `Email.Valor` (propriedade do ValueObject)
  - Tamanho máximo de `Nome` (200)
  - `Cpf` como não nulo
  - `Email` como não nulo
  - `Nome` como não nulo

#### W2.3: Criar Migration Inicial
- [x] Executar: `dotnet ef migrations add InitialCreate --project src/DesafioComIA.Infrastructure --startup-project src/DesafioComIA.Api`
- [x] Verificar arquivo de migration gerado
- [x] Validar SQL gerado para criação da tabela `Clientes`
- [x] Validar índices únicos para `Cpf.Valor` e `Email.Valor`

#### W2.4: Aplicar Migration
- [x] Executar: `dotnet ef database update --project src/DesafioComIA.Infrastructure --startup-project src/DesafioComIA.Api`
- [x] Verificar criação da tabela no PostgreSQL
- [x] Validar estrutura da tabela (colunas, índices, constraints)

#### W2.5: Criar DTOs Base
- [x] Criar pasta `DTOs` no projeto Application
- [x] Criar `ClienteDto`:
  - `Id` (Guid)
  - `Nome` (string)
  - `Cpf` (string)
  - `Email` (string)
- [x] Criar `CreateClienteDto`:
  - `Nome` (string)
  - `Cpf` (string)
  - `Email` (string)
- [x] Criar `ClienteListDto` (para listagem):
  - `Id` (Guid)
  - `Nome` (string)
  - `Cpf` (string)
  - `Email` (string)

#### W2.6: Configurar AutoMapper Profiles
- [x] Criar `ClienteProfile` no projeto Application
- [x] Configurar mapeamento `Cliente` → `ClienteDto`:
  - Mapear `Cpf.Valor` → `Cpf` (string)
  - Mapear `Email.Valor` → `Email` (string)
- [x] Configurar mapeamento `Cliente` → `ClienteListDto`:
  - Mapear `Cpf.Valor` → `Cpf` (string)
  - Mapear `Email.Valor` → `Email` (string)
- [x] Configurar mapeamento `CreateClienteDto` → `Cliente`:
  - Criar instância de `Cpf` ValueObject a partir da string
  - Criar instância de `Email` ValueObject a partir da string
- [x] Validar mapeamentos com testes unitários (opcional)

---

## 🌊 Wave 3: Commands (Write Operations) - TAR-001

### Objetivo
Implementar o cadastro de cliente utilizando o padrão CQRS com Commands.

### Microtarefas

#### W3.1: Criar CreateClienteCommand
- [x] Criar pasta `Commands/Cliente` no projeto Application
- [x] Criar `CreateClienteCommand` implementando `IMediatorCommand<ClienteDto>` do Mvp24Hours
- [x] Adicionar propriedades:
  - `Nome` (string, init)
  - `Cpf` (string, init)
  - `Email` (string, init)
- [x] Usar `record` para imutabilidade

#### W3.2: Criar CreateClienteCommandValidator
- [x] Criar `CreateClienteCommandValidator` herdando de `AbstractValidator<CreateClienteCommand>`
- [x] Implementar regras de validação:
  - `Nome`: Não vazio, mínimo 3 caracteres, máximo 200 caracteres
  - `Cpf`: Não vazio, usar validação do ValueObject `Cpf` do Mvp24Hours
  - `Email`: Não vazio, usar validação do ValueObject `Email` do Mvp24Hours
- [x] Adicionar mensagens de erro personalizadas em português
- [x] Usar métodos de validação dos ValueObjects `Cpf` e `Email` do Mvp24Hours

#### W3.3: Criar Exceções de Negócio
- [x] Criar exceção customizada `ClienteJaExisteException` herdando de `BusinessException` do Mvp24Hours
- [x] Criar exceção `ClienteNaoEncontradoException` herdando de `BusinessException` do Mvp24Hours (para uso futuro)
- [x] Adicionar mensagens de erro em português

#### W3.4: Criar CreateClienteCommandHandler
- [x] Criar `CreateClienteCommandHandler` implementando `IMediatorCommandHandler<CreateClienteCommand, ClienteDto>` do Mvp24Hours
- [x] Injetar dependências via construtor:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IUnitOfWorkAsync` do Mvp24Hours
  - `IMapper`
- [x] Implementar método `Handle`:
  - Criar instância de `Cpf` ValueObject a partir da string do comando
  - Criar instância de `Email` ValueObject a partir da string do comando
  - Validar se CPF já existe no banco (buscar por `Cpf.Valor`)
  - Validar se Email já existe no banco (buscar por `Email.Valor`)
  - Criar nova instância de `Cliente` com ValueObjects `Cpf` e `Email`
  - Adicionar ao repositório
  - Salvar mudanças com UnitOfWork
  - Mapear para DTO e retornar

#### W3.5: Implementar Validação de CPF Duplicado
- [x] No `CreateClienteCommandHandler`, antes de criar:
  - Normalizar CPF do comando usando ValueObject `Cpf` (já normaliza internamente)
  - Buscar cliente existente por `Cpf.Valor` usando repositório
  - Se existir, lançar `ClienteJaExisteException` com mensagem apropriada

#### W3.6: Implementar Validação de Email Duplicado
- [x] No `CreateClienteCommandHandler`, antes de criar:
  - Criar instância de `Email` ValueObject a partir da string do comando (já normaliza internamente)
  - Buscar cliente existente por `Email.Valor` usando repositório
  - Se existir, lançar `ClienteJaExisteException` com mensagem apropriada

#### W3.7: Criar Controller para CreateClienteCommand
- [x] Criar `ClientesController` no projeto API
- [x] Injetar `ISender` (do Mvp24Hours) via construtor
- [x] Criar endpoint `POST /api/clientes`:
  - Receber `CreateClienteDto` no body
  - Mapear para `CreateClienteCommand`
  - Enviar comando via `ISender.SendAsync()`
  - Retornar `201 Created` com `ClienteDto` no body
  - Tratar exceções de validação e negócio (via middleware)

#### W3.8: Configurar Swagger para Endpoint de Create
- [x] Adicionar atributos `[ApiController]` e `[Route("api/[controller]")]` no controller
- [x] Adicionar `[ProducesResponseType]` para documentação Swagger:
  - `201 Created` com `ClienteDto`
  - `400 Bad Request` para validação
  - `409 Conflict` para CPF/Email duplicado
  - `500 Internal Server Error`
- [x] Atualizar middleware para tratar `ClienteJaExisteException` como 409 Conflict

#### W3.9: Testes de Integração - Cadastro Válido
- [x] Criar projeto de teste de integração (`DesafioComIA.Api.IntegrationTests`) seguindo padrão xUnit, usando SDK tests/netcore, em solution separada (ou subpasta `tests/`), referenciando o projeto API principal
- [x] Criar teste de integração para cadastro com dados válidos
- [x] Validar resposta 201 Created
- [x] Validar dados retornados
- [x] Validar persistência no banco

#### W3.9.1: Configurar TestContainers para PostgreSQL nos testes de integração
- [x] Adicionar pacote `Testcontainers.PostgreSql` no projeto de testes de integração
- [x] Criar base fixture/configuração para iniciar container do PostgreSQL para os testes
- [x] Garantir que a string de conexão dos testes utilize o banco do container
- [x] Ajustar setup (`CustomWebApplicationFactory`) para consumir a string de conexão dinâmica vinda do container
- [x] Garantir teardown e limpeza do ambiente após os testes

#### W3.10: Testes de Integração - Validações
- [x] Teste: CPF duplicado retorna 409 Conflict
- [x] Teste: Email duplicado retorna 409 Conflict
- [x] Teste: CPF inválido retorna 400 Bad Request
- [x] Teste: Email inválido retorna 400 Bad Request
- [x] Teste: Nome muito curto retorna 400 Bad Request
- [x] Teste: Nome muito longo retorna 400 Bad Request

---

## 🌊 Wave 4: Queries (Read Operations) - TAR-002, TAR-003, TAR-004, TAR-005, TAR-006

### Objetivo
Implementar listagem e filtros de clientes utilizando o padrão CQRS com Queries.

### Microtarefas

#### W4.1: Usar PagedResult<T> do Mvp24Hours
- [x] Mvp24Hours já fornece `PagedResult<T>` no namespace `Mvp24Hours.Application.Logic.Pagination`
- [x] Utilizar `PagedResult<T>` do Mvp24Hours nas queries e handlers

#### W4.2: Criar ListClientesQuery (TAR-002)
- [x] Criar pasta `Queries/Cliente` no projeto Application
- [x] Criar `ListClientesQuery` implementando `IMediatorQuery<PagedResult<ClienteListDto>>` do Mvp24Hours
- [x] Adicionar propriedades de paginação:
  - `Page` (int, padrão 1)
  - `PageSize` (int, padrão 10, máximo 100)
- [x] Adicionar propriedades de ordenação:
  - `SortBy` (string, opcional, padrão "Nome")
  - `Descending` (bool, padrão false)

#### W4.3: Criar ListClientesQueryHandler (TAR-002)
- [x] Criar `ListClientesQueryHandler` implementando `IMediatorQueryHandler<ListClientesQuery, PagedResult<ClienteListDto>>` do Mvp24Hours
- [x] Injetar dependências:
  - `IRepositoryAsync<Cliente>`
  - `IMapper`
- [x] Implementar método `Handle`:
  - Criar `PagingCriteriaExpression` do Mvp24Hours com offset e limit
  - Configurar ordenação por Nome (ascendente por padrão)
  - Buscar clientes com paginação usando `ListAsync` do repositório
  - Contar total de registros usando `ListCountAsync` do repositório
  - Mapear para `ClienteListDto` (mapear `Cpf` e `Email` ValueObjects para strings via AutoMapper)
  - Retornar `PagedResult` com items, currentPage, pageSize, totalCount

#### W4.4: Criar GetClientesQuery com Filtros (TAR-003, TAR-004, TAR-005, TAR-006)
- [x] Criar `GetClientesQuery` implementando `IMediatorQuery<PagedResult<ClienteListDto>>` do Mvp24Hours
- [x] Adicionar propriedades de filtro:
  - `Nome` (string, opcional) - busca parcial, case-insensitive
  - `Cpf` (string, opcional) - busca exata, aceita com/sem formatação
  - `Email` (string, opcional) - busca exata, case-insensitive
- [x] Adicionar propriedades de paginação:
  - `Page` (int, padrão 1)
  - `PageSize` (int, padrão 10, máximo 100)
- [x] Adicionar propriedades de ordenação:
  - `SortBy` (string, opcional, padrão "Nome")
  - `Descending` (bool, padrão false)

#### W4.5: Criar GetClientesQueryValidator
- [x] Criar `GetClientesQueryValidator` herdando de `AbstractValidator<GetClientesQuery>`
- [x] Implementar regras:
  - `Page`: Maior que 0
  - `PageSize`: Entre 1 e 100
  - `Cpf`: Se informado, deve ter formato válido (pode ter formatação)
  - `Email`: Se informado, deve ter formato básico válido

#### W4.6: Criar GetClientesQueryHandler (TAR-003, TAR-004, TAR-005, TAR-006)
- [x] Criar `GetClientesQueryHandler` implementando `IMediatorQueryHandler<GetClientesQuery, PagedResult<ClienteListDto>>` do Mvp24Hours
- [x] Injetar dependências:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IMapper`
- [x] Implementar método `Handle`:
  - Criar expressão de filtro dinâmica baseada nos parâmetros usando `Expression<Func<Cliente, bool>>`
  - Aplicar filtro de Nome (parcial, case-insensitive) se informado: `c => c.Nome.ToLower().Contains(nome.ToLower())`
  - Aplicar filtro de CPF (exato) se informado: criar `Cpf` ValueObject e filtrar por `c => c.Cpf == cpfValueObject`
  - Aplicar filtro de Email (exato) se informado: criar `Email` ValueObject e filtrar por `c => c.Email == emailValueObject`
  - Combinar filtros com operador AND usando `Expression.AndAlso`
  - Criar `PagingCriteriaExpression` do Mvp24Hours com ordenação
  - Buscar clientes filtrados com paginação usando `GetByAsync` do repositório
  - Contar total de registros filtrados usando `GetByCountAsync` do repositório
  - Mapear para `ClienteListDto` (mapear ValueObjects para strings via AutoMapper)
  - Retornar `PagedResult`

#### W4.7: Implementar Filtro por Nome (TAR-003)
- [x] No `GetClientesQueryHandler`:
  - Se `Nome` informado, normalizar (trim)
  - Se vazio após normalização, ignorar filtro
  - Criar expressão: `c => c.Nome.ToLower().Contains(nomeNormalizado.ToLower())`
  - Aplicar filtro na query usando `GetByAsync` com expressão

#### W4.8: Implementar Filtro por CPF (TAR-004)
- [x] No `GetClientesQueryHandler`:
  - Se `Cpf` informado, criar instância de `Cpf` ValueObject do Mvp24Hours (já normaliza internamente)
  - Validar formato usando o ValueObject
  - Criar expressão: `c => c.Cpf == cpfValueObject`
  - Aplicar filtro na query usando `GetByAsync` com expressão

#### W4.9: Implementar Filtro por Email (TAR-005)
- [x] No `GetClientesQueryHandler`:
  - Se `Email` informado, criar instância de `Email` ValueObject do Mvp24Hours (já normaliza internamente)
  - Validar formato usando o ValueObject
  - Criar expressão: `c => c.Email == emailValueObject`
  - Aplicar filtro na query usando `GetByAsync` com expressão

#### W4.10: Implementar Combinação de Filtros (TAR-006)
- [x] No `GetClientesQueryHandler`:
  - Criar lista de expressões de filtro
  - Adicionar filtro de Nome se informado
  - Adicionar filtro de CPF se informado
  - Adicionar filtro de Email se informado
  - Combinar todas as expressões com operador AND usando `Expression.AndAlso`
  - Aplicar filtro combinado na query

#### W4.11: Implementar Ordenação Customizada
- [x] No `GetClientesQueryHandler`:
  - Validar `SortBy` (deve ser uma propriedade válida de Cliente: Nome, Cpf, Email)
  - Criar `PagingCriteriaExpression` do Mvp24Hours
  - Configurar ordenação usando `OrderByAscendingExpr` ou `OrderByDescendingExpr` do Mvp24Hours
  - Se `Descending` true, usar `OrderByDescendingExpr`
  - Se `Descending` false, usar `OrderByAscendingExpr`
  - Aplicar ordenação na query através do `PagingCriteriaExpression`

#### W4.12: Criar Endpoints no Controller
- [x] Adicionar endpoint `GET /api/clientes`:
  - Aceitar query parameters: `page`, `pageSize`, `sortBy`, `descending`
  - Criar `ListClientesQuery` com parâmetros
  - Enviar query via Mediator
  - Retornar `200 OK` com `PagedResult<ClienteListDto>`
- [x] Adicionar endpoint `GET /api/clientes/search`:
  - Aceitar query parameters: `nome`, `cpf`, `email`, `page`, `pageSize`, `sortBy`, `descending`
  - Criar `GetClientesQuery` com parâmetros
  - Enviar query via Mediator
  - Retornar `200 OK` com `PagedResult<ClienteListDto>`

#### W4.13: Configurar Swagger para Endpoints de Query
- [x] Adicionar `[ProducesResponseType]` para documentação:
  - `200 OK` com `PagedResult<ClienteListDto>`
  - `400 Bad Request` para validação
  - `500 Internal Server Error`
- [x] Adicionar `[FromQuery]` nos parâmetros do endpoint
- [x] Adicionar comentários XML para documentação Swagger

#### W4.14: Testes de Integração - Listagem Sem Filtros
- [x] Teste: Listar todos os clientes retorna 200 OK
- [x] Teste: Paginação funciona corretamente
- [x] Teste: Ordenação por nome funciona (ascendente por padrão)
- [x] Teste: Lista vazia retorna array vazio com totalCount = 0

#### W4.15: Testes de Integração - Filtro por Nome (TAR-003)
- [x] Teste: Busca parcial encontra clientes corretos
- [x] Teste: Busca é case-insensitive
- [x] Teste: Busca ignora espaços em branco no início/fim
- [x] Teste: Termo vazio retorna todos os clientes

#### W4.16: Testes de Integração - Filtro por CPF (TAR-004)
- [x] Teste: Busca exata encontra cliente correto
- [x] Teste: Aceita CPF com formatação (123.456.789-00)
- [x] Teste: Aceita CPF sem formatação (12345678900)
- [x] Teste: CPF inexistente retorna lista vazia
- [x] Teste: CPF inválido retorna 400 Bad Request

#### W4.17: Testes de Integração - Filtro por Email (TAR-005)
- [x] Teste: Busca exata encontra cliente correto
- [x] Teste: Busca é case-insensitive
- [x] Teste: Email inexistente retorna lista vazia
- [x] Teste: Email inválido retorna 400 Bad Request
- [x] Teste: Ignora espaços em branco no início/fim

#### W4.18: Testes de Integração - Combinação de Filtros (TAR-006)
- [x] Teste: Filtro Nome + CPF retorna apenas clientes que atendem ambos
- [x] Teste: Filtro Nome + Email retorna apenas clientes que atendem ambos
- [x] Teste: Filtro CPF + Email retorna apenas clientes que atendem ambos
- [x] Teste: Filtro Nome + CPF + Email retorna apenas clientes que atendem todos
- [x] Teste: Nenhum cliente atende todos os critérios retorna lista vazia

---

## 📝 Notas de Implementação

### Estrutura de Pastas Recomendada

```
src/
├── DesafioComIA.Api/
│   ├── Controllers/
│   │   └── ClientesController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   └── Program.cs
├── DesafioComIA.Application/
│   ├── Commands/
│   │   └── Cliente/
│   │       ├── CreateClienteCommand.cs
│   │       ├── CreateClienteCommandValidator.cs
│   │       └── CreateClienteCommandHandler.cs
│   ├── Queries/
│   │   └── Cliente/
│   │       ├── ListClientesQuery.cs
│   │       ├── ListClientesQueryHandler.cs
│   │       ├── GetClientesQuery.cs
│   │       ├── GetClientesQueryValidator.cs
│   │       └── GetClientesQueryHandler.cs
│   ├── DTOs/
│   │   ├── ClienteDto.cs
│   │   ├── CreateClienteDto.cs
│   │   └── ClienteListDto.cs
│   ├── Mappings/
│   │   └── ClienteProfile.cs
│   └── Models/
│       └── PagedResult.cs
├── DesafioComIA.Domain/
│   └── Entities/
│       └── Cliente.cs
└── DesafioComIA.Infrastructure/
    ├── Data/
    │   ├── ApplicationDbContext.cs
    │   └── Migrations/
    └── Helpers/
        └── (helpers de infraestrutura se necessário)
```

### Validação e Normalização de Dados

- **CPF**: Usar ValueObject `Cpf` do Mvp24Hours que já implementa validação e normalização
- **Email**: Usar ValueObject `Email` do Mvp24Hours que já implementa validação e normalização
- **Nome**: Remover espaços extras, manter capitalização inicial

### Tratamento de Erros

Usar `BusinessException` do Mvp24Hours ou criar exceções customizadas:
- `ClienteJaExisteException` para CPF/Email duplicado (herdar de `BusinessException`)
- `ClienteNaoEncontradoException` para cliente não encontrado (herdar de `BusinessException`)
- Validações de entrada usando FluentValidation (integração com Mvp24Hours Mediator)

### Performance

- Usar `NoTracking` para queries de leitura quando possível
- Implementar cache para queries frequentes (opcional)
- Usar índices no banco de dados (já configurados para CPF e Email)

---

## ✅ Checklist de Conclusão

- [x] Wave 1: Arquitetura base configurada
- [x] Wave 2: Entidade e contexto criados
- [x] Wave 3: Commands implementados (TAR-001)
- [x] Wave 4: Queries implementadas (TAR-002 a TAR-006)
- [x] Testes de integração passando (32 testes - 100% de sucesso)
- [x] Documentação Swagger completa
- [x] Migrations aplicadas no banco de dados
- [x] Health checks funcionando
- [x] Tratamento de erros implementado

---

## 📚 Referências

- [Mvp24Hours Documentation](https://github.com/mvp24hours)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [PostgreSQL .NET Documentation](https://www.npgsql.org/efcore/)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [AutoMapper Documentation](https://docs.automapper.org/)
