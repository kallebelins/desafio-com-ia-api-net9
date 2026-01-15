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
- [ ] Criar projeto WebAPI .NET 9: `dotnet new webapi -n DesafioComIA.Api`
- [ ] Criar estrutura de pastas:
  - `src/DesafioComIA.Api/` (API Layer)
  - `src/DesafioComIA.Application/` (Application Layer - Commands/Queries)
  - `src/DesafioComIA.Domain/` (Domain Layer - Entities)
  - `src/DesafioComIA.Infrastructure/` (Infrastructure Layer - Data Access)
- [ ] Configurar solution: `dotnet new sln -n DesafioComIA`
- [ ] Adicionar projetos à solution
- [ ] Configurar referências entre projetos:
  - API → Application, Infrastructure
  - Application → Infrastructure, Domain
  - Infrastructure → Domain

#### W1.2: Instalar Pacotes NuGet - Core
- [ ] Instalar `Mvp24Hours.Core` (versão 9.*) em todos os projetos necessários
- [ ] Instalar `Mvp24Hours.Application` (versão 9.*) no projeto Application
- [ ] Instalar `Mvp24Hours.Infrastructure.Data.EFCore` (versão 9.*) no projeto Infrastructure
- [ ] Instalar `Mvp24Hours.Infrastructure.Cqrs` (versão 9.*) no projeto Application
- [ ] Instalar `Mvp24Hours.WebAPI` (versão 9.*) no projeto API

#### W1.3: Instalar Pacotes NuGet - PostgreSQL
- [ ] Instalar `Npgsql.EntityFrameworkCore.PostgreSQL` (versão 9.*) no projeto Infrastructure
- [ ] Instalar `Microsoft.EntityFrameworkCore.Design` (versão 9.*) no projeto Infrastructure
- [ ] Instalar `Microsoft.EntityFrameworkCore.Tools` (versão 9.*) no projeto Infrastructure

#### W1.4: Instalar Pacotes NuGet - Validação e Mapeamento
- [ ] Instalar `FluentValidation` (versão 11.*) no projeto Application
- [ ] Instalar `FluentValidation.AspNetCore` (versão 11.*) no projeto API
- [ ] Instalar `AutoMapper` (versão 12.*) no projeto Application
- [ ] Instalar `AutoMapper.Extensions.Microsoft.DependencyInjection` no projeto Application

#### W1.5: Configurar appsettings.json
- [ ] Criar arquivo `appsettings.json` no projeto API
- [ ] Adicionar ConnectionString para PostgreSQL:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Port=5432;Pooling=true;Database=DesafioComIA;User Id=postgres;Password=postgres;"
    }
  }
  ```
- [ ] Criar `appsettings.Development.json` com configurações de desenvolvimento
- [ ] Criar `appsettings.Production.json` com configurações de produção

#### W1.6: Configurar Program.cs - Base
- [ ] Configurar builder do WebApplication
- [ ] Configurar logging básico
- [ ] Configurar CORS (se necessário)
- [ ] Configurar Swagger/OpenAPI

#### W1.7: Configurar Program.cs - PostgreSQL e DbContext
- [ ] Criar classe `ApplicationDbContext` no projeto Infrastructure herdando de `Mvp24HoursContext`
- [ ] Configurar DbContext no Program.cs:
  ```csharp
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
  ```
- [ ] Registrar Mvp24Hours DbContext:
  ```csharp
  builder.Services.AddMvp24HoursDbContext<ApplicationDbContext>();
  ```
- [ ] Registrar Repository Async:
  ```csharp
  builder.Services.AddMvp24HoursRepositoryAsync(options =>
  {
      options.MaxQtyByQueryPage = 100;
      options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
  });
  ```

#### W1.8: Configurar Program.cs - CQRS Mediator
- [ ] Registrar Mediator:
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
- [ ] Registrar FluentValidation:
  ```csharp
  builder.Services.AddValidatorsFromAssemblyContaining<Program>();
  builder.Services.AddFluentValidationAutoValidation();
  builder.Services.AddFluentValidationClientsideAdapters();
  ```

#### W1.10: Configurar Program.cs - AutoMapper
- [ ] Registrar AutoMapper:
  ```csharp
  builder.Services.AddAutoMapper(typeof(Program).Assembly);
  ```

#### W1.11: Configurar Health Checks
- [ ] Adicionar Health Checks para PostgreSQL:
  ```csharp
  builder.Services.AddHealthChecks()
      .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"), name: "postgresql");
  ```
- [ ] Configurar endpoint de health check: `/health`

#### W1.12: Configurar Exception Handling
- [ ] Criar middleware de tratamento de exceções
- [ ] Configurar ProblemDetails para respostas de erro
- [ ] Mapear exceções de validação para ProblemDetails
- [ ] Mapear exceções de negócio para ProblemDetails

---

## 🌊 Wave 2: Entidade e Contexto de Dados

### Objetivo
Criar a entidade Cliente, configurar o DbContext e preparar as migrations do banco de dados.

### Microtarefas

#### W2.1: Criar Entidade Cliente
- [ ] Criar classe `Cliente` no projeto Domain
- [ ] Herdar de `EntityBase<Guid>` do Mvp24Hours
- [ ] Implementar propriedades:
  - `Id` (Guid) - herdado de EntityBase
  - `Nome` (string, obrigatório, 3-200 caracteres)
  - `Cpf` (ValueObject Cpf do Mvp24Hours, obrigatório, único)
  - `Email` (ValueObject Email do Mvp24Hours, obrigatório, único)
  - `CreatedAt` (DateTime) - herdado de EntityBase
  - `ModifiedAt` (DateTime?) - herdado de EntityBase
- [ ] Adicionar construtor padrão
- [ ] Adicionar construtor com parâmetros principais
- [ ] Usar ValueObjects `Cpf` e `Email` já existentes do Mvp24Hours

#### W2.2: Configurar Entity no DbContext
- [ ] Abrir `ApplicationDbContext` no projeto Infrastructure
- [ ] Criar `DbSet<Cliente> Clientes { get; set; }`
- [ ] Configurar `OnModelCreating`:
  - Configurar nome da tabela: `"Clientes"`
  - Configurar chave primária: `Id`
  - Configurar índice único para `Cpf.Valor` (propriedade do ValueObject)
  - Configurar índice único para `Email.Valor` (propriedade do ValueObject)
  - Configurar tamanho máximo de `Nome` (200)
  - Configurar `Cpf` como não nulo (ValueObject)
  - Configurar `Email` como não nulo (ValueObject)
  - Configurar `Nome` como não nulo

#### W2.3: Criar Migration Inicial
- [ ] Executar: `dotnet ef migrations add InitialCreate --project src/DesafioComIA.Infrastructure --startup-project src/DesafioComIA.Api`
- [ ] Verificar arquivo de migration gerado
- [ ] Validar SQL gerado para criação da tabela `Clientes`
- [ ] Validar índices únicos para `Cpf.Valor` e `Email.Valor`

#### W2.4: Aplicar Migration
- [ ] Executar: `dotnet ef database update --project src/DesafioComIA.Infrastructure --startup-project src/DesafioComIA.Api`
- [ ] Verificar criação da tabela no PostgreSQL
- [ ] Validar estrutura da tabela (colunas, índices, constraints)

#### W2.5: Criar DTOs Base
- [ ] Criar pasta `DTOs` no projeto Application
- [ ] Criar `ClienteDto`:
  - `Id` (Guid)
  - `Nome` (string)
  - `Cpf` (string)
  - `Email` (string)
- [ ] Criar `CreateClienteDto`:
  - `Nome` (string)
  - `Cpf` (string)
  - `Email` (string)
- [ ] Criar `ClienteListDto` (para listagem):
  - `Id` (Guid)
  - `Nome` (string)
  - `Cpf` (string)
  - `Email` (string)

#### W2.6: Configurar AutoMapper Profiles
- [ ] Criar `ClienteProfile` no projeto Application
- [ ] Configurar mapeamento `Cliente` → `ClienteDto`:
  - Mapear `Cpf.Valor` → `Cpf` (string)
  - Mapear `Email.Valor` → `Email` (string)
- [ ] Configurar mapeamento `Cliente` → `ClienteListDto`:
  - Mapear `Cpf.Valor` → `Cpf` (string)
  - Mapear `Email.Valor` → `Email` (string)
- [ ] Configurar mapeamento `CreateClienteDto` → `Cliente`:
  - Criar instância de `Cpf` ValueObject a partir da string
  - Criar instância de `Email` ValueObject a partir da string
- [ ] Validar mapeamentos com testes unitários (opcional)

---

## 🌊 Wave 3: Commands (Write Operations) - TAR-001

### Objetivo
Implementar o cadastro de cliente utilizando o padrão CQRS com Commands.

### Microtarefas

#### W3.1: Criar CreateClienteCommand
- [ ] Criar pasta `Commands/Cliente` no projeto Application
- [ ] Criar `CreateClienteCommand` implementando `IMediatorCommand<ClienteDto>` do Mvp24Hours
- [ ] Adicionar propriedades:
  - `Nome` (string, init)
  - `Cpf` (string, init)
  - `Email` (string, init)
- [ ] Usar `record` para imutabilidade

#### W3.2: Criar CreateClienteCommandValidator
- [ ] Criar `CreateClienteCommandValidator` herdando de `AbstractValidator<CreateClienteCommand>`
- [ ] Implementar regras de validação:
  - `Nome`: Não vazio, mínimo 3 caracteres, máximo 200 caracteres
  - `Cpf`: Não vazio, usar validação do ValueObject `Cpf` do Mvp24Hours
  - `Email`: Não vazio, usar validação do ValueObject `Email` do Mvp24Hours
- [ ] Adicionar mensagens de erro personalizadas em português
- [ ] Usar métodos de validação dos ValueObjects `Cpf` e `Email` do Mvp24Hours

#### W3.3: Criar Exceções de Negócio
- [ ] Criar exceção customizada `ClienteJaExisteException` herdando de `BusinessException` do Mvp24Hours
- [ ] Criar exceção `ClienteNaoEncontradoException` herdando de `BusinessException` do Mvp24Hours (para uso futuro)
- [ ] Adicionar mensagens de erro em português

#### W3.4: Criar CreateClienteCommandHandler
- [ ] Criar `CreateClienteCommandHandler` implementando `IMediatorCommandHandler<CreateClienteCommand, ClienteDto>` do Mvp24Hours
- [ ] Injetar dependências via construtor:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IUnitOfWorkAsync` do Mvp24Hours
  - `IMapper`
- [ ] Implementar método `Handle`:
  - Criar instância de `Cpf` ValueObject a partir da string do comando
  - Criar instância de `Email` ValueObject a partir da string do comando
  - Validar se CPF já existe no banco (buscar por `Cpf.Valor`)
  - Validar se Email já existe no banco (buscar por `Email.Valor`)
  - Criar nova instância de `Cliente` com ValueObjects `Cpf` e `Email`
  - Adicionar ao repositório
  - Salvar mudanças com UnitOfWork
  - Mapear para DTO e retornar

#### W3.5: Implementar Validação de CPF Duplicado
- [ ] No `CreateClienteCommandHandler`, antes de criar:
  - Normalizar CPF do comando usando ValueObject `Cpf` (já normaliza internamente)
  - Buscar cliente existente por `Cpf.Valor` usando repositório
  - Se existir, lançar `ClienteJaExisteException` com mensagem apropriada

#### W3.6: Implementar Validação de Email Duplicado
- [ ] No `CreateClienteCommandHandler`, antes de criar:
  - Criar instância de `Email` ValueObject a partir da string do comando (já normaliza internamente)
  - Buscar cliente existente por `Email.Valor` usando repositório
  - Se existir, lançar `ClienteJaExisteException` com mensagem apropriada

#### W3.7: Criar Controller para CreateClienteCommand
- [ ] Criar `ClientesController` no projeto API
- [ ] Injetar `IMediator` (do Mvp24Hours) via construtor
- [ ] Criar endpoint `POST /api/clientes`:
  - Receber `CreateClienteDto` no body
  - Mapear para `CreateClienteCommand`
  - Enviar comando via `IMediator.SendAsync()`
  - Retornar `201 Created` com `ClienteDto` no body
  - Tratar exceções de validação e negócio

#### W3.8: Configurar Swagger para Endpoint de Create
- [ ] Adicionar atributos `[ApiController]` e `[Route("api/[controller]")]` no controller
- [ ] Adicionar `[ProducesResponseType]` para documentação Swagger:
  - `201 Created` com `ClienteDto`
  - `400 Bad Request` para validação
  - `409 Conflict` para CPF/Email duplicado
  - `500 Internal Server Error`

#### W3.9: Testes de Integração - Cadastro Válido
- [ ] Criar teste de integração para cadastro com dados válidos
- [ ] Validar resposta 201 Created
- [ ] Validar dados retornados
- [ ] Validar persistência no banco

#### W3.10: Testes de Integração - Validações
- [ ] Teste: CPF duplicado retorna 409 Conflict
- [ ] Teste: Email duplicado retorna 409 Conflict
- [ ] Teste: CPF inválido retorna 400 Bad Request
- [ ] Teste: Email inválido retorna 400 Bad Request
- [ ] Teste: Nome muito curto retorna 400 Bad Request
- [ ] Teste: Nome muito longo retorna 400 Bad Request

---

## 🌊 Wave 4: Queries (Read Operations) - TAR-002, TAR-003, TAR-004, TAR-005, TAR-006

### Objetivo
Implementar listagem e filtros de clientes utilizando o padrão CQRS com Queries.

### Microtarefas

#### W4.1: Criar PagedResult<T> Helper (se necessário)
- [ ] Verificar se Mvp24Hours já fornece `PagedResult<T>` ou similar
- [ ] Se não existir, criar classe `PagedResult<T>` no projeto Application:
  - `Items` (IEnumerable<T>)
  - `TotalCount` (int)
  - `Page` (int)
  - `PageSize` (int)
  - `TotalPages` (int, calculado)
  - `HasPreviousPage` (bool, calculado)
  - `HasNextPage` (bool, calculado)

#### W4.2: Criar ListClientesQuery (TAR-002)
- [ ] Criar pasta `Queries/Cliente` no projeto Application
- [ ] Criar `ListClientesQuery` implementando `IMediatorQuery<PagedResult<ClienteListDto>>` do Mvp24Hours
- [ ] Adicionar propriedades de paginação:
  - `Page` (int, padrão 1)
  - `PageSize` (int, padrão 10, máximo 100)
- [ ] Adicionar propriedades de ordenação:
  - `SortBy` (string, opcional, padrão "Nome")
  - `Descending` (bool, padrão false)

#### W4.3: Criar ListClientesQueryHandler (TAR-002)
- [ ] Criar `ListClientesQueryHandler` implementando `IMediatorQueryHandler<ListClientesQuery, PagedResult<ClienteListDto>>` do Mvp24Hours
- [ ] Injetar dependências:
  - `IRepositoryAsync<Cliente>`
  - `IMapper`
- [ ] Implementar método `Handle`:
  - Criar `PagingCriteria` do Mvp24Hours com page e pageSize
  - Configurar ordenação por Nome (ascendente por padrão)
  - Buscar clientes com paginação usando `GetByAsync` do repositório
  - Contar total de registros usando `CountAsync` do repositório
  - Mapear para `ClienteListDto` (mapear `Cpf.Valor` e `Email.Valor` para strings)
  - Retornar `PagedResult` com items, totalCount, page, pageSize

#### W4.4: Criar GetClientesQuery com Filtros (TAR-003, TAR-004, TAR-005, TAR-006)
- [ ] Criar `GetClientesQuery` implementando `IMediatorQuery<PagedResult<ClienteListDto>>` do Mvp24Hours
- [ ] Adicionar propriedades de filtro:
  - `Nome` (string, opcional) - busca parcial, case-insensitive
  - `Cpf` (string, opcional) - busca exata, aceita com/sem formatação
  - `Email` (string, opcional) - busca exata, case-insensitive
- [ ] Adicionar propriedades de paginação:
  - `Page` (int, padrão 1)
  - `PageSize` (int, padrão 10, máximo 100)
- [ ] Adicionar propriedades de ordenação:
  - `SortBy` (string, opcional, padrão "Nome")
  - `Descending` (bool, padrão false)

#### W4.5: Criar GetClientesQueryValidator
- [ ] Criar `GetClientesQueryValidator` herdando de `AbstractValidator<GetClientesQuery>`
- [ ] Implementar regras:
  - `Page`: Maior que 0
  - `PageSize`: Entre 1 e 100
  - `Cpf`: Se informado, deve ter formato válido (pode ter formatação)
  - `Email`: Se informado, deve ter formato básico válido

#### W4.6: Criar GetClientesQueryHandler (TAR-003, TAR-004, TAR-005, TAR-006)
- [ ] Criar `GetClientesQueryHandler` implementando `IMediatorQueryHandler<GetClientesQuery, PagedResult<ClienteListDto>>` do Mvp24Hours
- [ ] Injetar dependências:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IMapper`
- [ ] Implementar método `Handle`:
  - Criar expressão de filtro dinâmica baseada nos parâmetros usando `Expression<Func<Cliente, bool>>`
  - Aplicar filtro de Nome (parcial, case-insensitive) se informado: `c => c.Nome.Contains(nome)`
  - Aplicar filtro de CPF (exato) se informado: criar `Cpf` ValueObject e filtrar por `c => c.Cpf.Valor == cpf.Valor`
  - Aplicar filtro de Email (exato) se informado: criar `Email` ValueObject e filtrar por `c => c.Email.Valor == email.Valor`
  - Combinar filtros com operador AND usando `PredicateBuilder` ou `Expression.AndAlso`
  - Criar `PagingCriteria` do Mvp24Hours com ordenação
  - Buscar clientes filtrados com paginação usando `GetByAsync` do repositório
  - Contar total de registros filtrados usando `CountAsync` do repositório
  - Mapear para `ClienteListDto` (mapear `Cpf.Valor` e `Email.Valor` para strings)
  - Retornar `PagedResult`

#### W4.7: Implementar Filtro por Nome (TAR-003)
- [ ] No `GetClientesQueryHandler`:
  - Se `Nome` informado, normalizar (trim)
  - Se vazio após normalização, ignorar filtro
  - Criar expressão: `c => c.Nome.ToLower().Contains(nomeNormalizado.ToLower())`
  - Aplicar filtro na query usando `GetByAsync` com expressão

#### W4.8: Implementar Filtro por CPF (TAR-004)
- [ ] No `GetClientesQueryHandler`:
  - Se `Cpf` informado, criar instância de `Cpf` ValueObject do Mvp24Hours (já normaliza internamente)
  - Validar formato usando o ValueObject
  - Criar expressão: `c => c.Cpf.Valor == cpf.Valor`
  - Aplicar filtro na query usando `GetByAsync` com expressão

#### W4.9: Implementar Filtro por Email (TAR-005)
- [ ] No `GetClientesQueryHandler`:
  - Se `Email` informado, criar instância de `Email` ValueObject do Mvp24Hours (já normaliza internamente)
  - Validar formato usando o ValueObject
  - Criar expressão: `c => c.Email.Valor == email.Valor`
  - Aplicar filtro na query usando `GetByAsync` com expressão

#### W4.10: Implementar Combinação de Filtros (TAR-006)
- [ ] No `GetClientesQueryHandler`:
  - Criar lista de expressões de filtro
  - Adicionar filtro de Nome se informado
  - Adicionar filtro de CPF se informado
  - Adicionar filtro de Email se informado
  - Combinar todas as expressões com operador AND usando `PredicateBuilder` ou `Expression.AndAlso`
  - Aplicar filtro combinado na query

#### W4.11: Implementar Ordenação Customizada
- [ ] No `GetClientesQueryHandler`:
  - Validar `SortBy` (deve ser uma propriedade válida de Cliente: Nome, Cpf.Valor, Email.Valor)
  - Criar `PagingCriteria` do Mvp24Hours
  - Configurar ordenação usando `OrderByAscendingExpr` ou `OrderByDescendingExpr` do Mvp24Hours
  - Se `Descending` true, usar `OrderByDescendingExpr`
  - Se `Descending` false, usar `OrderByAscendingExpr`
  - Aplicar ordenação na query através do `PagingCriteria`

#### W4.12: Criar Endpoints no Controller
- [ ] Adicionar endpoint `GET /api/clientes`:
  - Aceitar query parameters: `page`, `pageSize`, `sortBy`, `descending`
  - Aceitar query parameters de filtro: `nome`, `cpf`, `email`
  - Criar `GetClientesQuery` com parâmetros
  - Enviar query via Mediator
  - Retornar `200 OK` com `PagedResult<ClienteListDto>`
- [ ] Adicionar endpoint `GET /api/clientes/{id}` (opcional, para buscar por ID):
  - Criar `GetClienteByIdQuery`
  - Criar `GetClienteByIdQueryHandler`
  - Retornar `200 OK` com `ClienteDto` ou `404 Not Found`

#### W4.13: Configurar Swagger para Endpoints de Query
- [ ] Adicionar `[ProducesResponseType]` para documentação:
  - `200 OK` com `PagedResult<ClienteListDto>`
  - `400 Bad Request` para validação
  - `500 Internal Server Error`
- [ ] Adicionar `[FromQuery]` nos parâmetros do endpoint
- [ ] Adicionar comentários XML para documentação Swagger

#### W4.14: Testes de Integração - Listagem Sem Filtros
- [ ] Teste: Listar todos os clientes retorna 200 OK
- [ ] Teste: Paginação funciona corretamente
- [ ] Teste: Ordenação por nome funciona (ascendente por padrão)
- [ ] Teste: Lista vazia retorna array vazio com totalCount = 0

#### W4.15: Testes de Integração - Filtro por Nome (TAR-003)
- [ ] Teste: Busca parcial encontra clientes corretos
- [ ] Teste: Busca é case-insensitive
- [ ] Teste: Busca ignora espaços em branco no início/fim
- [ ] Teste: Termo vazio retorna todos os clientes

#### W4.16: Testes de Integração - Filtro por CPF (TAR-004)
- [ ] Teste: Busca exata encontra cliente correto
- [ ] Teste: Aceita CPF com formatação (123.456.789-00)
- [ ] Teste: Aceita CPF sem formatação (12345678900)
- [ ] Teste: CPF inexistente retorna lista vazia
- [ ] Teste: CPF inválido retorna 400 Bad Request

#### W4.17: Testes de Integração - Filtro por Email (TAR-005)
- [ ] Teste: Busca exata encontra cliente correto
- [ ] Teste: Busca é case-insensitive
- [ ] Teste: Email inexistente retorna lista vazia
- [ ] Teste: Email inválido retorna 400 Bad Request
- [ ] Teste: Ignora espaços em branco no início/fim

#### W4.18: Testes de Integração - Combinação de Filtros (TAR-006)
- [ ] Teste: Filtro Nome + CPF retorna apenas clientes que atendem ambos
- [ ] Teste: Filtro Nome + Email retorna apenas clientes que atendem ambos
- [ ] Teste: Filtro CPF + Email retorna apenas clientes que atendem ambos
- [ ] Teste: Filtro Nome + CPF + Email retorna apenas clientes que atendem todos
- [ ] Teste: Nenhum cliente atende todos os critérios retorna lista vazia
- [ ] Teste: Ordem dos filtros não afeta resultado

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

- [ ] Wave 1: Arquitetura base configurada
- [ ] Wave 2: Entidade e contexto criados
- [ ] Wave 3: Commands implementados (TAR-001)
- [ ] Wave 4: Queries implementadas (TAR-002 a TAR-006)
- [ ] Testes de integração passando
- [ ] Documentação Swagger completa
- [ ] Migrations aplicadas no banco de dados
- [ ] Health checks funcionando
- [ ] Tratamento de erros implementado

---

## 📚 Referências

- [Mvp24Hours Documentation](https://github.com/mvp24hours)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [PostgreSQL .NET Documentation](https://www.npgsql.org/efcore/)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [AutoMapper Documentation](https://docs.automapper.org/)
