# ADR-002: Padrão Arquitetural para APIs RESTful

**Status:** ✅ Aceito  
**Data da Decisão:** 16 de Janeiro de 2026  
**Contexto:** Desafio com IA - API .NET 9  
**Arquiteto/Responsável:** Kallebe Lins  

---

## 📋 Sumário Executivo

Este documento estabelece os **padrões arquiteturais obrigatórios** para todos os serviços e endpoints da aplicação. Toda nova funcionalidade, módulo ou recurso DEVE seguir estas diretrizes.

### Escopo de Aplicação
- ✅ Todos os controllers da API
- ✅ Todos os endpoints RESTful
- ✅ Todos os Commands e Queries (CQRS)
- ✅ Todos os DTOs e modelos de resposta
- ✅ Toda documentação Swagger/OpenAPI
- ✅ Todos os tratamentos de erro

### Objetivo
Garantir **consistência, manutenibilidade e conformidade RESTful** em toda a aplicação através de padrões arquiteturais bem definidos e testados.

---

## 🎯 Contexto e Motivação

### Problema
APIs inconsistentes geram:
- ❌ Experiência ruim para desenvolvedores
- ❌ Dificuldade de manutenção
- ❌ Documentação confusa
- ❌ Integração complexa
- ❌ Bugs e comportamentos inesperados

### Solução Adotada
Definir padrões arquiteturais claros baseados em:
- ✅ Princípios RESTful (RFC 7231, RFC 5789, RFC 7807)
- ✅ CQRS com Mvp24Hours Framework
- ✅ .NET 9 Best Practices
- ✅ OpenAPI/Swagger Standards

### Caso de Referência
O módulo **Clientes** foi implementado seguindo estes padrões e serve como referência para todos os futuros módulos.

---

## 🏛️ Decisões Arquiteturais

## 1. Padrão de Rotas RESTful

### ADR-002.1: Estrutura de URLs

**Decisão:** Todas as rotas DEVEM seguir o padrão RESTful hierárquico e usar substantivos no plural.

#### ✅ Padrão Obrigatório

```
/api/{recursos}              → Collection (listagem)
/api/{recursos}/{id}         → Item (recurso específico)
/api/{recursos}/search       → Collection com filtros (aceitável, mas não ideal)
/api/{recursos}/{id}/{sub}   → Sub-recurso relacionado
```

#### Exemplos Corretos

```
GET    /api/clientes              → Listar todos os clientes
POST   /api/clientes              → Criar novo cliente
GET    /api/clientes/{id}         → Obter cliente específico
PUT    /api/clientes/{id}         → Atualizar cliente completo
PATCH  /api/clientes/{id}         → Atualizar cliente parcial
DELETE /api/clientes/{id}         → Remover cliente
GET    /api/clientes/search       → Buscar clientes com filtros
```

#### ❌ Exemplos Incorretos (NÃO FAZER)

```
❌ /api/criarCliente              → Usa verbo (não RESTful)
❌ /api/cliente                   → Singular (deveria ser plural)
❌ /api/clientes/deletar/{id}     → Usa verbo na URL
❌ /api/getCliente/{id}           → Usa verbo (GET já indica ação)
❌ /api/cliente-update            → Formato incorreto
```

#### Razão
- URLs devem representar **recursos**, não **ações**
- Métodos HTTP (GET, POST, PUT, DELETE) representam as ações
- Plural facilita compreensão de collection vs item
- Hierarquia clara facilita versionamento e expansão

---

### ADR-002.2: Mapeamento de Operações CRUD

**Decisão:** Usar métodos HTTP semânticos conforme tabela abaixo.

#### Operações Obrigatórias para Recursos Completos

| Operação | Método HTTP | Rota | Status Success | Idempotente | Seguro |
|----------|-------------|------|----------------|-------------|--------|
| **Listar** | GET | `/api/{recursos}` | 200 OK | ✅ Sim | ✅ Sim |
| **Criar** | POST | `/api/{recursos}` | 201 Created | ❌ Não | ❌ Não |
| **Obter** | GET | `/api/{recursos}/{id}` | 200 OK | ✅ Sim | ✅ Sim |
| **Atualizar Completo** | PUT | `/api/{recursos}/{id}` | 200 OK | ✅ Sim | ❌ Não |
| **Atualizar Parcial** | PATCH | `/api/{recursos}/{id}` | 200 OK | ✅ Sim | ❌ Não |
| **Remover** | DELETE | `/api/{recursos}/{id}` | 204 No Content | ✅ Sim | ❌ Não |
| **Buscar** | GET | `/api/{recursos}/search` | 200 OK | ✅ Sim | ✅ Sim |

#### Características Obrigatórias

**Idempotência:**
- ✅ GET, PUT, PATCH, DELETE DEVEM ser idempotentes
- ❌ POST NÃO deve ser idempotente
- Múltiplas requisições idênticas = mesmo estado final

**Segurança (Safe):**
- ✅ GET DEVE ser seguro (não modifica estado)
- ❌ POST, PUT, PATCH, DELETE NÃO são seguros (modificam estado)

**Razão:**
- Garante previsibilidade para clientes da API
- Permite retry seguro de operações idempotentes
- Facilita caching de operações seguras
- Segue RFC 7231 (HTTP/1.1 Semantics)

---

### ADR-002.3: Status Codes HTTP

**Decisão:** Usar códigos de status HTTP conforme especificação oficial e matriz abaixo.

#### Matriz Obrigatória de Status Codes

| Operação | Sucesso | Validação | Não Encontrado | Conflito | Erro Interno |
|----------|---------|-----------|----------------|----------|--------------|
| **POST /recursos** | 201 Created | 400 | - | 409 | 500 |
| **GET /recursos** | 200 OK | 400 | - | - | 500 |
| **GET /recursos/search** | 200 OK | 400 | - | - | 500 |
| **GET /recursos/{id}** | 200 OK | 400 | 404 | - | 500 |
| **PUT /recursos/{id}** | 200 OK | 400 | 404 | 409 | 500 |
| **PATCH /recursos/{id}** | 200 OK | 400 | 404 | 409 | 500 |
| **DELETE /recursos/{id}** | 204 No Content | 400 | 404 | - | 500 |

#### Significado dos Status Codes

| Código | Nome | Quando Usar | Body |
|--------|------|-------------|------|
| **200 OK** | Sucesso com corpo | GET, PUT, PATCH | DTO do recurso |
| **201 Created** | Recurso criado | POST | DTO do recurso + Location header |
| **204 No Content** | Sucesso sem corpo | DELETE | Vazio |
| **400 Bad Request** | Erro de validação | Dados inválidos/faltantes | ProblemDetails |
| **404 Not Found** | Recurso não existe | GET/PUT/PATCH/DELETE de ID inexistente | ProblemDetails |
| **409 Conflict** | Conflito de estado | Duplicidade (CPF, Email, etc.) | ProblemDetails |
| **500 Internal Server Error** | Erro não tratado | Exceção inesperada | ProblemDetails |

#### Regras Específicas

**201 Created (POST):**
- ✅ DEVE incluir header `Location` com URL do recurso criado
- ✅ DEVE retornar o recurso criado no body
- ❌ NÃO usar 200 OK para criação

**204 No Content (DELETE):**
- ✅ DEVE ter body vazio
- ❌ NÃO usar 200 OK para deleção
- ⚠️ Segunda tentativa de DELETE retorna 404 (recurso já não existe)

**400 Bad Request:**
- ✅ Validação de parâmetros (required, formato, range)
- ✅ Corpo da requisição inválido
- ✅ Pelo menos um campo obrigatório em PATCH

**404 Not Found:**
- ✅ GET/PUT/PATCH/DELETE de ID que não existe
- ❌ NÃO usar para erros de validação

**409 Conflict:**
- ✅ CPF/Email/Username duplicado
- ✅ Violação de constraint única
- ✅ Conflito de versão (concorrência otimista)

#### Razão
- Códigos de status semânticos facilitam integração
- Clientes podem tratar erros de forma consistente
- Segue RFC 7231 e boas práticas RESTful
- ProblemDetails (RFC 7807) fornece detalhes padronizados

---

## 2. Padrão CQRS com Mvp24Hours

### ADR-002.4: Separação Command/Query

**Decisão:** TODA operação DEVE ser implementada usando CQRS com Mvp24Hours.

#### Estrutura Obrigatória

```
Application/
├── Commands/
│   └── {Recurso}/
│       ├── {Operacao}Command.cs           → Record imutável
│       ├── {Operacao}CommandValidator.cs  → FluentValidation
│       └── {Operacao}CommandHandler.cs    → Lógica de negócio
│
└── Queries/
    └── {Recurso}/
        ├── {Operacao}Query.cs             → Record imutável
        ├── {Operacao}QueryValidator.cs    → FluentValidation
        └── {Operacao}QueryHandler.cs      → Lógica de consulta
```

#### Exemplo: Módulo Cliente

```
Application/Commands/Cliente/
├── CreateClienteCommand.cs
├── CreateClienteCommandValidator.cs
├── CreateClienteCommandHandler.cs
├── UpdateClienteCommand.cs
├── UpdateClienteCommandValidator.cs
├── UpdateClienteCommandHandler.cs
├── PatchClienteCommand.cs
├── PatchClienteCommandValidator.cs
├── PatchClienteCommandHandler.cs
├── DeleteClienteCommand.cs
├── DeleteClienteCommandValidator.cs
└── DeleteClienteCommandHandler.cs

Application/Queries/Cliente/
├── ListClientesQuery.cs
├── ListClientesQueryHandler.cs
├── GetClientesQuery.cs
├── GetClientesQueryHandler.cs
├── GetClientesQueryValidator.cs
├── GetClienteByIdQuery.cs
├── GetClienteByIdQueryValidator.cs
└── GetClienteByIdQueryHandler.cs
```

#### Regras de Implementação

**Commands (Modificam Estado):**
- ✅ DEVE implementar `IMediatorCommand<TResult>` do Mvp24Hours
- ✅ DEVE usar `record` para imutabilidade
- ✅ DEVE ter Validator com FluentValidation
- ✅ DEVE ter Handler com lógica de negócio
- ✅ Handler DEVE injetar `IUnitOfWorkAsync` para persistência
- ✅ Handler DEVE injetar `IRepositoryAsync<T>` para acesso a dados

**Queries (Não Modificam Estado):**
- ✅ DEVE implementar `IMediatorQuery<TResult>` do Mvp24Hours
- ✅ DEVE usar `record` para imutabilidade
- ✅ DEVE ter Validator se houver parâmetros complexos
- ✅ DEVE ter Handler com lógica de consulta
- ✅ Handler DEVE injetar `IRepositoryAsync<T>` para acesso a dados
- ❌ NÃO deve injetar `IUnitOfWorkAsync` (queries não persistem)

#### Template de Command

```csharp
// Command (imutável)
public record CreateClienteCommand : IMediatorCommand<ClienteDto>
{
    public string Nome { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

// Validator
public class CreateClienteCommandValidator : AbstractValidator<CreateClienteCommand>
{
    public CreateClienteCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório")
            .Must(BeValidCpf).WithMessage("CPF inválido");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");
    }

    private bool BeValidCpf(string cpf)
    {
        return Mvp24Hours.Core.ValueObjects.Logic.Cpf.TryParse(cpf, out _);
    }
}

// Handler
public class CreateClienteCommandHandler : IMediatorCommandHandler<CreateClienteCommand, ClienteDto>
{
    private readonly IRepositoryAsync<Cliente> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateClienteCommandHandler> _logger;

    public CreateClienteCommandHandler(
        IRepositoryAsync<Cliente> repository,
        IUnitOfWorkAsync unitOfWork,
        IMapper mapper,
        ILogger<CreateClienteCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ClienteDto> Handle(CreateClienteCommand command, CancellationToken cancellationToken)
    {
        // 1. Validar unicidade
        var cpf = Mvp24Hours.Core.ValueObjects.Logic.Cpf.Parse(command.Cpf);
        var existingByCpf = await _repository.GetAsync(
            x => x.Cpf == cpf, 
            cancellationToken: cancellationToken);
        
        if (existingByCpf.Any())
            throw new ClienteJaExisteException("CPF já cadastrado");

        var email = Mvp24Hours.Core.ValueObjects.Logic.Email.Parse(command.Email);
        var existingByEmail = await _repository.GetAsync(
            x => x.Email == email, 
            cancellationToken: cancellationToken);
        
        if (existingByEmail.Any())
            throw new ClienteJaExisteException("Email já cadastrado");

        // 2. Criar entidade
        var cliente = new Cliente(
            nome: command.Nome,
            cpf: cpf,
            email: email
        );

        // 3. Persistir
        _repository.Add(cliente);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Retornar DTO
        return _mapper.Map<ClienteDto>(cliente);
    }
}
```

#### Template de Query

```csharp
// Query (imutável)
public record GetClienteByIdQuery : IMediatorQuery<ClienteDto>
{
    public Guid Id { get; init; }
}

// Validator
public class GetClienteByIdQueryValidator : AbstractValidator<GetClienteByIdQuery>
{
    public GetClienteByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty).WithMessage("ID inválido");
    }
}

// Handler
public class GetClienteByIdQueryHandler : IMediatorQueryHandler<GetClienteByIdQuery, ClienteDto>
{
    private readonly IRepositoryAsync<Cliente> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetClienteByIdQueryHandler> _logger;

    public GetClienteByIdQueryHandler(
        IRepositoryAsync<Cliente> repository,
        IMapper mapper,
        ILogger<GetClienteByIdQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ClienteDto> Handle(GetClienteByIdQuery query, CancellationToken cancellationToken)
    {
        var cliente = await _repository.GetByIdAsync(query.Id, cancellationToken);
        
        if (cliente == null)
            throw new ClienteNaoEncontradoException($"Cliente com ID {query.Id} não encontrado");

        return _mapper.Map<ClienteDto>(cliente);
    }
}
```

#### Razão
- Separação clara entre leitura e escrita
- Validação centralizada e reutilizável
- Facilita testes unitários
- Permite otimizações específicas (queries podem usar projeções)
- Facilita evolução e manutenção

---

### ADR-002.5: Validação com FluentValidation

**Decisão:** TODA validação de entrada DEVE usar FluentValidation integrado ao Mvp24Hours.

#### Regras de Validação Obrigatórias

**Campos Obrigatórios:**
```csharp
RuleFor(x => x.Nome)
    .NotEmpty().WithMessage("Campo obrigatório")
    .NotNull().WithMessage("Campo obrigatório");
```

**Strings:**
```csharp
RuleFor(x => x.Nome)
    .NotEmpty()
    .MinimumLength(3).WithMessage("Mínimo 3 caracteres")
    .MaximumLength(200).WithMessage("Máximo 200 caracteres");
```

**Guids:**
```csharp
RuleFor(x => x.Id)
    .NotEqual(Guid.Empty).WithMessage("ID inválido");
```

**ValueObjects do Mvp24Hours:**
```csharp
// CPF
RuleFor(x => x.Cpf)
    .NotEmpty()
    .Must(cpf => Mvp24Hours.Core.ValueObjects.Logic.Cpf.TryParse(cpf, out _))
    .WithMessage("CPF inválido");

// Email
RuleFor(x => x.Email)
    .NotEmpty()
    .Must(email => Mvp24Hours.Core.ValueObjects.Logic.Email.TryParse(email, out _))
    .WithMessage("Email inválido");
```

**Paginação:**
```csharp
RuleFor(x => x.Page)
    .GreaterThanOrEqualTo(1).WithMessage("Página deve ser >= 1");

RuleFor(x => x.PageSize)
    .GreaterThanOrEqualTo(1).WithMessage("PageSize deve ser >= 1")
    .LessThanOrEqualTo(100).WithMessage("PageSize deve ser <= 100");
```

**PATCH (Pelo Menos Um Campo):**
```csharp
RuleFor(x => x)
    .Must(x => x.Nome != null || x.Cpf != null || x.Email != null)
    .WithMessage("Pelo menos um campo deve ser informado");
```

#### Mensagens de Erro
- ✅ DEVE ser em português
- ✅ DEVE ser clara e específica
- ✅ DEVE indicar o que está errado e como corrigir
- ❌ NÃO expor detalhes internos ou técnicos

#### Razão
- Validação declarativa e legível
- Mensagens de erro consistentes
- Fácil manutenção e teste
- Integração automática com Mvp24Hours

---

## 3. Padrão de Controllers

### ADR-002.6: Estrutura de Controllers

**Decisão:** Todos os controllers DEVEM seguir estrutura mínima e usar CQRS via `ISender`.

#### Template Obrigatório

```csharp
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Application.Logic.Pagination;
using DesafioComIA.Application.Commands.{Recurso};
using DesafioComIA.Application.Queries.{Recurso};
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class {Recursos}Controller : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<{Recursos}Controller> _logger;

    public {Recursos}Controller(
        ISender sender,
        ILogger<{Recursos}Controller> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    // Endpoints aqui...
}
```

#### Características Obrigatórias

**Atributos:**
- ✅ `[ApiController]` - Ativa validação automática e binding
- ✅ `[Route("api/[controller]")]` - Rota base
- ✅ `[Produces("application/json")]` - Tipo de conteúdo

**Dependências:**
- ✅ `ISender` do Mvp24Hours - Para enviar Commands/Queries
- ✅ `ILogger<T>` - Para logging estruturado
- ❌ NÃO injetar repositórios diretamente (usar CQRS)
- ❌ NÃO injetar DbContext diretamente (usar CQRS)

**Métodos:**
- ✅ DEVE ser `async Task<ActionResult<T>>`
- ✅ DEVE aceitar `CancellationToken`
- ✅ DEVE ter XML comments para Swagger
- ✅ DEVE ter `[ProducesResponseType]` para todos os status codes

#### Razão
- Consistência em todos os controllers
- Facilita manutenção e testes
- Integração automática com Swagger
- Logging centralizado

---

### ADR-002.7: Estrutura de Endpoints

**Decisão:** Cada endpoint DEVE seguir template específico por operação.

#### POST - Criar Recurso

```csharp
/// <summary>
/// Cria um novo {recurso}
/// </summary>
/// <param name="dto">Dados do {recurso} a ser criado</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>{Recurso} criado</returns>
/// <response code="201">{Recurso} criado com sucesso</response>
/// <response code="400">Erro de validação</response>
/// <response code="409">Conflito (duplicidade)</response>
/// <response code="500">Erro interno do servidor</response>
[HttpPost]
[ProducesResponseType(typeof({Recurso}Dto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<{Recurso}Dto>> Create(
    [FromBody] Create{Recurso}Dto dto,
    CancellationToken cancellationToken)
{
    var command = new Create{Recurso}Command
    {
        // Mapear propriedades
    };

    var result = await _sender.SendAsync(command, cancellationToken);

    return CreatedAtAction(
        nameof(GetById),
        new { id = result.Id },
        result);
}
```

**Pontos-chave:**
- ✅ Retorna `201 Created`
- ✅ Inclui `Location` header via `CreatedAtAction`
- ✅ Body contém o recurso criado

#### GET Collection - Listar

```csharp
/// <summary>
/// Lista todos os {recursos} com paginação
/// </summary>
/// <param name="page">Número da página (padrão: 1)</param>
/// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
/// <param name="sortBy">Campo de ordenação</param>
/// <param name="descending">Ordenação descendente</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>Lista paginada de {recursos}</returns>
/// <response code="200">Lista retornada com sucesso</response>
/// <response code="400">Erro de validação</response>
/// <response code="500">Erro interno do servidor</response>
[HttpGet]
[ProducesResponseType(typeof(PagedResult<{Recurso}ListDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<PagedResult<{Recurso}ListDto>>> List(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string sortBy = "Nome",
    [FromQuery] bool descending = false,
    CancellationToken cancellationToken = default)
{
    var query = new List{Recursos}Query
    {
        Page = page,
        PageSize = pageSize,
        SortBy = sortBy,
        Descending = descending
    };

    var result = await _sender.SendAsync(query, cancellationToken);
    return Ok(result);
}
```

**Pontos-chave:**
- ✅ Retorna `PagedResult<T>` do Mvp24Hours
- ✅ Parâmetros opcionais com valores padrão
- ✅ Máximo de 100 itens por página (validar no Query)

#### GET Item - Obter por ID

```csharp
/// <summary>
/// Obtém um {recurso} específico por ID
/// </summary>
/// <param name="id">ID do {recurso}</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>{Recurso} encontrado</returns>
/// <response code="200">{Recurso} encontrado com sucesso</response>
/// <response code="400">ID inválido</response>
/// <response code="404">{Recurso} não encontrado</response>
/// <response code="500">Erro interno do servidor</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof({Recurso}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<{Recurso}Dto>> GetById(
    [FromRoute] Guid id,
    CancellationToken cancellationToken)
{
    var query = new Get{Recurso}ByIdQuery { Id = id };
    var result = await _sender.SendAsync(query, cancellationToken);
    return Ok(result);
}
```

**Pontos-chave:**
- ✅ ID na rota com `[FromRoute]`
- ✅ Retorna 404 se não encontrado (via exception no handler)

#### PUT - Atualizar Completo

```csharp
/// <summary>
/// Atualiza um {recurso} completamente
/// </summary>
/// <param name="id">ID do {recurso}</param>
/// <param name="command">Dados completos do {recurso}</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>{Recurso} atualizado</returns>
/// <response code="200">{Recurso} atualizado com sucesso</response>
/// <response code="400">Erro de validação</response>
/// <response code="404">{Recurso} não encontrado</response>
/// <response code="409">Conflito (duplicidade)</response>
/// <response code="500">Erro interno do servidor</response>
[HttpPut("{id}")]
[ProducesResponseType(typeof({Recurso}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<{Recurso}Dto>> Update(
    [FromRoute] Guid id,
    [FromBody] Update{Recurso}Command command,
    CancellationToken cancellationToken)
{
    // Garantir que ID da rota corresponde ao comando
    command = command with { Id = id };
    
    var result = await _sender.SendAsync(command, cancellationToken);
    return Ok(result);
}
```

**Pontos-chave:**
- ✅ Todos os campos obrigatórios no command
- ✅ ID da rota sobrescreve ID do body (segurança)
- ✅ Idempotente (mesma requisição = mesmo resultado)

#### PATCH - Atualizar Parcial

```csharp
/// <summary>
/// Atualiza um {recurso} parcialmente
/// </summary>
/// <param name="id">ID do {recurso}</param>
/// <param name="command">Campos a serem atualizados (pelo menos um obrigatório)</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>{Recurso} atualizado</returns>
/// <response code="200">{Recurso} atualizado com sucesso</response>
/// <response code="400">Erro de validação ou nenhum campo informado</response>
/// <response code="404">{Recurso} não encontrado</response>
/// <response code="409">Conflito (duplicidade)</response>
/// <response code="500">Erro interno do servidor</response>
[HttpPatch("{id}")]
[ProducesResponseType(typeof({Recurso}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<{Recurso}Dto>> Patch(
    [FromRoute] Guid id,
    [FromBody] Patch{Recurso}Command command,
    CancellationToken cancellationToken)
{
    command = command with { Id = id };
    
    var result = await _sender.SendAsync(command, cancellationToken);
    return Ok(result);
}
```

**Pontos-chave:**
- ✅ Campos opcionais (nullable) no command
- ✅ Validar que pelo menos um campo foi informado
- ✅ Campos null = não atualizar

#### DELETE - Remover

```csharp
/// <summary>
/// Remove um {recurso}
/// </summary>
/// <param name="id">ID do {recurso}</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>Sem conteúdo em caso de sucesso</returns>
/// <response code="204">{Recurso} removido com sucesso</response>
/// <response code="400">ID inválido</response>
/// <response code="404">{Recurso} não encontrado</response>
/// <response code="500">Erro interno do servidor</response>
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> Delete(
    [FromRoute] Guid id,
    CancellationToken cancellationToken)
{
    var command = new Delete{Recurso}Command { Id = id };
    await _sender.SendAsync(command, cancellationToken);
    
    return NoContent();
}
```

**Pontos-chave:**
- ✅ Retorna `204 No Content` (SEM body)
- ✅ Tipo de retorno `IActionResult` (não genérico)
- ✅ Segunda tentativa retorna 404 (idempotente em estado, não em status)

#### GET Search - Buscar com Filtros

```csharp
/// <summary>
/// Busca {recursos} com filtros opcionais
/// </summary>
/// <param name="filtro1">Descrição do filtro 1</param>
/// <param name="filtro2">Descrição do filtro 2</param>
/// <param name="page">Número da página (padrão: 1)</param>
/// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
/// <param name="sortBy">Campo de ordenação</param>
/// <param name="descending">Ordenação descendente</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>Lista paginada de {recursos} filtrados</returns>
/// <response code="200">Lista retornada com sucesso</response>
/// <response code="400">Erro de validação</response>
/// <response code="500">Erro interno do servidor</response>
[HttpGet("search")]
[ProducesResponseType(typeof(PagedResult<{Recurso}ListDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<PagedResult<{Recurso}ListDto>>> Search(
    [FromQuery] string? filtro1 = null,
    [FromQuery] string? filtro2 = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string sortBy = "Nome",
    [FromQuery] bool descending = false,
    CancellationToken cancellationToken = default)
{
    var query = new Get{Recursos}Query
    {
        Filtro1 = filtro1,
        Filtro2 = filtro2,
        Page = page,
        PageSize = pageSize,
        SortBy = sortBy,
        Descending = descending
    };

    var result = await _sender.SendAsync(query, cancellationToken);
    return Ok(result);
}
```

**Pontos-chave:**
- ✅ Filtros opcionais (nullable)
- ✅ Retorna `PagedResult<T>`
- ✅ Mesma estrutura de paginação do List

---

## 4. Padrão de DTOs

### ADR-002.8: Estrutura de DTOs

**Decisão:** Usar DTOs específicos para entrada/saída e nunca expor entidades diretamente.

#### Tipos de DTOs Obrigatórios

**1. Create DTO (Input para POST):**
```csharp
public record Create{Recurso}Dto
{
    public string Campo1 { get; init; } = string.Empty;
    public string Campo2 { get; init; } = string.Empty;
    // Apenas campos necessários para criação (SEM ID)
}
```

**2. Output DTO (Completo):**
```csharp
public record {Recurso}Dto
{
    public Guid Id { get; init; }
    public string Campo1 { get; init; } = string.Empty;
    public string Campo2 { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    // Todos os campos necessários para cliente
}
```

**3. List DTO (Resumido):**
```csharp
public record {Recurso}ListDto
{
    public Guid Id { get; init; }
    public string Campo1 { get; init; } = string.Empty;
    public string Campo2 { get; init; } = string.Empty;
    // Apenas campos relevantes para listagem (otimização)
}
```

#### Regras de DTOs

- ✅ DEVE usar `record` para imutabilidade
- ✅ DEVE ter propriedades com `init` (imutáveis após criação)
- ✅ DEVE ter valores padrão para strings (`= string.Empty`)
- ✅ DEVE ter XML comments para Swagger
- ❌ NÃO expor entidades de domínio diretamente
- ❌ NÃO incluir lógica de negócio
- ❌ NÃO usar herança complexa

#### Mapeamento com AutoMapper

```csharp
public class {Recurso}Profile : Profile
{
    public {Recurso}Profile()
    {
        // Entidade → DTO completo
        CreateMap<{Recurso}, {Recurso}Dto>()
            .ForMember(d => d.Campo1, opt => opt.MapFrom(s => s.Campo1.ToString()));

        // Entidade → DTO de lista
        CreateMap<{Recurso}, {Recurso}ListDto>();

        // CreateDTO → Command (se necessário)
        CreateMap<Create{Recurso}Dto, Create{Recurso}Command>();
    }
}
```

#### Razão
- Desacoplamento entre API e domínio
- Controle sobre dados expostos
- Facilita versionamento
- Otimização de queries (projection)

---

## 5. Padrão de Exceções e ProblemDetails

### ADR-002.9: Tratamento de Erros

**Decisão:** Usar exceções customizadas e ProblemDetails (RFC 7807) para todos os erros.

#### Exceções de Domínio Obrigatórias

```csharp
// Recurso não encontrado → 404
public class {Recurso}NaoEncontradoException : Exception
{
    public {Recurso}NaoEncontradoException(string message) : base(message) { }
}

// Recurso duplicado → 409
public class {Recurso}JaExisteException : Exception
{
    public {Recurso}JaExisteException(string message) : base(message) { }
}
```

#### Middleware de Exception Handling

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            ValidationException => (400, "Erro de validação"),
            {Recurso}NaoEncontradoException => (404, "Recurso não encontrado"),
            {Recurso}JaExisteException => (409, "Conflito"),
            _ => (500, "Erro interno do servidor")
        };

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        // Adicionar traceId para rastreabilidade
        problemDetails.Extensions["traceId"] = Activity.Current?.Id 
            ?? context.TraceIdentifier;

        // Adicionar erros de validação se FluentValidation
        if (exception is ValidationException validationEx)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

#### Estrutura de ProblemDetails

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Recurso não encontrado",
  "status": 404,
  "detail": "Cliente com ID 123e4567-e89b-12d3-a456-426614174000 não encontrado",
  "instance": "/api/clientes/123e4567-e89b-12d3-a456-426614174000",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
  "errors": {
    "Nome": ["Nome é obrigatório", "Nome deve ter no mínimo 3 caracteres"],
    "Email": ["Email inválido"]
  }
}
```

#### Regras de ProblemDetails

- ✅ DEVE seguir RFC 7807
- ✅ DEVE incluir `type`, `title`, `status`, `detail`, `instance`
- ✅ DEVE incluir `traceId` para rastreabilidade
- ✅ DEVE incluir `errors` para validação (FluentValidation)
- ❌ NÃO expor stack traces em produção
- ❌ NÃO expor detalhes internos do sistema
- ❌ NÃO expor dados sensíveis

#### Razão
- Padrão RFC para erros
- Facilita integração com clientes
- Rastreabilidade com traceId
- Informações estruturadas sobre erros

---

## 6. Padrão de Documentação

### ADR-002.10: Swagger/OpenAPI

**Decisão:** Toda API DEVE ser completamente documentada com Swagger/OpenAPI.

#### Configuração Obrigatória do Swagger

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Desafio com IA API",
        Version = "v1",
        Description = "API RESTful para gerenciamento de recursos",
        Contact = new OpenApiContact
        {
            Name = "Kallebe Lins",
            Email = "email@example.com"
        }
    });

    // Incluir XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Configurar exemplos de ProblemDetails
    options.MapType<ProblemDetails>(() => new OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["type"] = new() { Type = "string" },
            ["title"] = new() { Type = "string" },
            ["status"] = new() { Type = "integer" },
            ["detail"] = new() { Type = "string" },
            ["instance"] = new() { Type = "string" },
            ["traceId"] = new() { Type = "string" }
        }
    });
});

// Habilitar XML documentation no .csproj
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

#### XML Comments Obrigatórios

**Controllers:**
```csharp
/// <summary>
/// Descrição do endpoint
/// </summary>
/// <param name="param1">Descrição do parâmetro 1</param>
/// <param name="param2">Descrição do parâmetro 2</param>
/// <param name="cancellationToken">Token de cancelamento</param>
/// <returns>Descrição do retorno</returns>
/// <response code="200">Descrição do sucesso</response>
/// <response code="400">Descrição do erro de validação</response>
/// <response code="404">Descrição do não encontrado</response>
/// <response code="500">Descrição do erro interno</response>
```

**DTOs:**
```csharp
/// <summary>
/// Descrição do DTO
/// </summary>
public record ClienteDto
{
    /// <summary>
    /// ID único do cliente
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    public Guid Id { get; init; }

    /// <summary>
    /// Nome completo do cliente
    /// </summary>
    /// <example>João da Silva</example>
    public string Nome { get; init; } = string.Empty;
}
```

#### ProducesResponseType Obrigatório

```csharp
[ProducesResponseType(typeof({Recurso}Dto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
```

#### Razão
- Documentação automática e sempre atualizada
- Facilita integração para clientes
- Geração automática de clients (SDKs)
- OpenAPI é padrão da indústria

---

## 7. Padrão de Paginação

### ADR-002.11: PagedResult

**Decisão:** Usar `PagedResult<T>` do Mvp24Hours para TODAS as listagens.

#### Estrutura Obrigatória

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
```

#### Parâmetros de Paginação

```csharp
[FromQuery] int page = 1               // Mínimo: 1
[FromQuery] int pageSize = 10          // Mínimo: 1, Máximo: 100
[FromQuery] string sortBy = "Nome"     // Campo de ordenação
[FromQuery] bool descending = false    // Ordem descendente
```

#### Validação de Paginação

```csharp
RuleFor(x => x.Page)
    .GreaterThanOrEqualTo(1).WithMessage("Página deve ser >= 1");

RuleFor(x => x.PageSize)
    .GreaterThanOrEqualTo(1).WithMessage("PageSize deve ser >= 1")
    .LessThanOrEqualTo(100).WithMessage("PageSize deve ser <= 100");

RuleFor(x => x.SortBy)
    .Must(BeValidSortField).WithMessage("Campo de ordenação inválido");
```

#### Response de Paginação

```json
{
  "items": [/* array de items */],
  "page": 1,
  "pageSize": 10,
  "totalCount": 156,
  "totalPages": 16,
  "hasPrevious": false,
  "hasNext": true
}
```

#### Razão
- Estrutura consistente em todas as listagens
- Facilita navegação para clientes
- Evita sobrecarga do servidor
- Integração com UI de paginação

---

## 8. Resumo de Convenções

### Nomenclatura

| Elemento | Padrão | Exemplo |
|----------|--------|---------|
| **Controller** | `{Recursos}Controller` | `ClientesController` |
| **Rota Base** | `/api/{recursos}` | `/api/clientes` |
| **Command** | `{Acao}{Recurso}Command` | `CreateClienteCommand` |
| **Query** | `{Acao}{Recurso}Query` | `GetClienteByIdQuery` |
| **Validator** | `{Nome}Validator` | `CreateClienteCommandValidator` |
| **Handler** | `{Nome}Handler` | `CreateClienteCommandHandler` |
| **DTO** | `{Recurso}Dto` | `ClienteDto` |
| **List DTO** | `{Recurso}ListDto` | `ClienteListDto` |
| **Create DTO** | `Create{Recurso}Dto` | `CreateClienteDto` |
| **Exception** | `{Recurso}{Tipo}Exception` | `ClienteNaoEncontradoException` |

### Estrutura de Pastas

```
src/
├── DesafioComIA.Api/
│   ├── Controllers/
│   │   └── {Recursos}Controller.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   └── Program.cs
│
├── DesafioComIA.Application/
│   ├── Commands/
│   │   └── {Recurso}/
│   │       ├── {Acao}Command.cs
│   │       ├── {Acao}CommandValidator.cs
│   │       └── {Acao}CommandHandler.cs
│   ├── Queries/
│   │   └── {Recurso}/
│   │       ├── {Acao}Query.cs
│   │       ├── {Acao}QueryValidator.cs
│   │       └── {Acao}QueryHandler.cs
│   ├── DTOs/
│   │   ├── {Recurso}Dto.cs
│   │   ├── {Recurso}ListDto.cs
│   │   └── Create{Recurso}Dto.cs
│   ├── Mappings/
│   │   └── {Recurso}Profile.cs
│   └── Exceptions/
│       ├── {Recurso}NaoEncontradoException.cs
│       └── {Recurso}JaExisteException.cs
│
├── DesafioComIA.Domain/
│   └── Entities/
│       └── {Recurso}.cs
│
└── DesafioComIA.Infrastructure/
    └── Data/
        ├── ApplicationDbContext.cs
        └── Configurations/
            └── {Recurso}Configuration.cs
```

---

## 📊 Checklist de Implementação

Use este checklist ao criar um novo módulo/recurso:

### Planejamento
- [ ] Definir nome do recurso (plural para URLs)
- [ ] Listar operações necessárias (CRUD completo ou parcial)
- [ ] Identificar regras de negócio e validações
- [ ] Definir campos únicos (para validação de duplicidade)

### Domain Layer
- [ ] Criar entidade em `Domain/Entities/{Recurso}.cs`
- [ ] Configurar EF Core em `Infrastructure/Data/Configurations/{Recurso}Configuration.cs`
- [ ] Criar migration

### Application Layer - Commands
- [ ] Criar `CreateCommand`, `Validator` e `Handler`
- [ ] Criar `UpdateCommand`, `Validator` e `Handler`
- [ ] Criar `PatchCommand`, `Validator` e `Handler` (se necessário)
- [ ] Criar `DeleteCommand`, `Validator` e `Handler`

### Application Layer - Queries
- [ ] Criar `List{Recursos}Query` e `Handler`
- [ ] Criar `Get{Recursos}Query` e `Handler` (search com filtros)
- [ ] Criar `Get{Recurso}ByIdQuery`, `Validator` e `Handler`

### Application Layer - DTOs
- [ ] Criar `{Recurso}Dto` (output completo)
- [ ] Criar `{Recurso}ListDto` (output resumido)
- [ ] Criar `Create{Recurso}Dto` (input para POST)
- [ ] Criar AutoMapper Profile em `Mappings/{Recurso}Profile.cs`

### Application Layer - Exceptions
- [ ] Criar `{Recurso}NaoEncontradoException` (404)
- [ ] Criar `{Recurso}JaExisteException` (409)
- [ ] Atualizar `ExceptionHandlingMiddleware`

### API Layer - Controller
- [ ] Criar `{Recursos}Controller.cs` com estrutura base
- [ ] Implementar endpoint `POST /api/{recursos}` (Create)
- [ ] Implementar endpoint `GET /api/{recursos}` (List)
- [ ] Implementar endpoint `GET /api/{recursos}/search` (Search)
- [ ] Implementar endpoint `GET /api/{recursos}/{id}` (GetById)
- [ ] Implementar endpoint `PUT /api/{recursos}/{id}` (Update)
- [ ] Implementar endpoint `PATCH /api/{recursos}/{id}` (Patch)
- [ ] Implementar endpoint `DELETE /api/{recursos}/{id}` (Delete)

### Documentação
- [ ] Adicionar XML comments em todos os endpoints
- [ ] Adicionar XML comments em todos os DTOs
- [ ] Adicionar `[ProducesResponseType]` em todos os endpoints
- [ ] Verificar documentação no Swagger UI

### Testes
- [ ] Criar testes de integração para todos os endpoints
- [ ] Testar cenários de sucesso
- [ ] Testar cenários de erro (400, 404, 409)
- [ ] Testar validações
- [ ] Testar idempotência (PUT, PATCH, DELETE)

### Validação Final
- [ ] Todas as rotas seguem padrão RESTful
- [ ] Todos os status codes estão corretos
- [ ] ProblemDetails configurado para todos os erros
- [ ] Documentação Swagger completa
- [ ] Testes passando (100%)
- [ ] Code review realizado

---

## 🎓 Princípios RESTful - Referência Rápida

### Métodos HTTP

| Método | Uso | Idempotente | Seguro | Body Request | Body Response |
|--------|-----|-------------|--------|--------------|---------------|
| **GET** | Recuperar | ✅ | ✅ | ❌ | ✅ |
| **POST** | Criar | ❌ | ❌ | ✅ | ✅ |
| **PUT** | Substituir completo | ✅ | ❌ | ✅ | ✅ |
| **PATCH** | Atualizar parcial | ✅ | ❌ | ✅ | ✅ |
| **DELETE** | Remover | ✅ | ❌ | ❌ | ❌ |

### Idempotência

**Definição:** Múltiplas requisições idênticas produzem o mesmo resultado.

**Exemplos:**
- ✅ `PUT /clientes/123 { nome: "João" }` → Sempre deixa nome como "João"
- ✅ `DELETE /clientes/123` → Sempre resulta em cliente 123 não existir
- ❌ `POST /clientes { nome: "João" }` → Cria novo cliente a cada chamada

### Segurança (Safe)

**Definição:** Método não modifica estado do servidor.

- ✅ GET: Apenas lê, não modifica
- ❌ POST/PUT/PATCH/DELETE: Modificam estado

### Cache

- ✅ **Cacheable:** GET (pode ser cacheado)
- ❌ **Não Cacheable:** POST, PUT, PATCH, DELETE

---

## 📚 Referências

### RFCs
- **RFC 7231:** HTTP/1.1 Semantics and Content
- **RFC 5789:** PATCH Method for HTTP
- **RFC 7807:** Problem Details for HTTP APIs

### Frameworks
- **Mvp24Hours:** https://github.com/kallebelins/mvp24hours-dotnet
- **.NET 9:** https://learn.microsoft.com/dotnet/
- **FluentValidation:** https://fluentvalidation.net/
- **AutoMapper:** https://automapper.org/

### Padrões
- **CQRS:** Command Query Responsibility Segregation
- **REST:** Representational State Transfer
- **OpenAPI:** https://swagger.io/specification/

---

## ✅ Status de Aplicação

### Módulos Implementados Conforme Este ADR
- ✅ **Clientes** (referência completa)

### Módulos Futuros (DEVEM seguir este ADR)
- ⏳ Pedidos
- ⏳ Produtos
- ⏳ Categorias
- ⏳ Fornecedores
- ⏳ (Todos os futuros módulos)

---

## 🔄 Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | 16/01/2026 | Kallebe Lins | Versão inicial baseada no módulo Clientes |

---

## 📝 Notas Finais

### Quando Desviar Destes Padrões?

**Regra Geral:** NÃO DESVIE sem aprovação explícita.

**Exceções Permitidas:**
1. Requisitos de negócio específicos documentados
2. Limitações técnicas comprovadas
3. Performance crítica com justificativa
4. Integrações legadas (documentar workaround)

**Processo de Exceção:**
1. Documentar razão técnica ou de negócio
2. Propor solução alternativa
3. Obter aprovação do arquiteto
4. Documentar decisão em ADR separado

### Evolução Deste Documento

Este documento é **versionado** e deve ser atualizado quando:
- ✅ Novos padrões são adotados
- ✅ Padrões existentes são refinados
- ✅ Exceções recorrentes são identificadas
- ✅ Feedback da equipe sugere melhorias

### Conformidade

**Revisões de Código:**
- ✅ Pull requests DEVEM ser validados contra este ADR
- ✅ Desvios devem ser apontados e corrigidos
- ✅ Exceções devem ser documentadas no PR

**Auditorias:**
- Revisar conformidade mensalmente
- Atualizar documento conforme necessário
- Treinar novos membros da equipe

---

**Documento aprovado por:** Kallebe Lins  
**Data de aprovação:** 16 de Janeiro de 2026  
**Próxima revisão:** Após implementação de 3+ módulos adicionais

---

*Este documento estabelece os padrões arquiteturais obrigatórios para toda a aplicação. Desvios não autorizados serão rejeitados em code review.*
