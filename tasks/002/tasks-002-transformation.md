# Tasks Transformation - Padronização de Rotas, Cache e Observabilidade

## 📋 Visão Geral

Este documento contém todas as microtarefas detalhadas para implementação do backlog de padronização RESTful, cache e observabilidade utilizando:
- **Framework**: Mvp24Hours .NET 9
- **Cache**: Redis ou Hybrid Cache (.NET 9)
- **Observabilidade**: OpenTelemetry com Jaeger, Prometheus e Grafana
- **Infraestrutura**: Docker Compose

---

## 🌊 Wave 1: Padronização de Rotas RESTful (TAR-007)

### Objetivo
Padronizar todas as rotas da API de clientes conforme especificação RESTful, garantindo consistência e seguindo boas práticas de design de APIs.

### Pré-requisito: Consultar Mvp24Hours
**⚠️ IMPORTANTE**: Antes de implementar qualquer solução customizada, SEMPRE consulte as ferramentas do Mvp24Hours:
- Consulte o `mvp24h_cqrs_guide` com os tópicos `command`, `query`, `handler`, `validation` e `dto` para garantir implementação conforme padrão CQRS do Mvp24Hours;
- Revise o `mvp24h_infrastructure_guide` nos tópicos `webapi` e `webapi-advanced` para padronização dos endpoints e controllers CQRS;
- Aplique orientações de APIs modernas e tratamento de erros usando o `mvp24h_modernization_guide` (category `apis` com features `problem-details`);

### Microtarefas

#### W1.1: Analisar Estrutura Atual de Rotas ✅
- [x] Listar todas as rotas atuais do `ClientesController`
- [x] Identificar endpoints que não seguem padrão RESTful
- [x] Documentar rotas atuais:
  - `POST /api/clientes` (criar cliente) ✅
  - `GET /api/clientes` (listar clientes) ✅
  - `GET /api/clientes/search` (buscar clientes) ✅
- [x] Identificar rotas faltantes conforme especificação:
  - `GET /api/clientes/{id}` (obter cliente específico) ❌ Placeholder
  - `PUT /api/clientes/{id}` (atualizar cliente completo) ❌ Não existe
  - `PATCH /api/clientes/{id}` (atualizar cliente parcial) ❌ Não existe
  - `DELETE /api/clientes/{id}` (remover cliente) ❌ Não existe

**📄 ADR gerado:** `tasks/002/tasks-002-arch-decision.md` (ADR-002: Padrão Arquitetural para APIs RESTful)
**Importância:** Este documento estabelece os padrões obrigatórios que TODOS os futuros módulos devem seguir.

#### W1.2: Consultar Padrões Mvp24Hours para WebAPI ✅
- [x] **OBRIGATÓRIO**: Executar `mvp24h_infrastructure_guide` com topic `webapi` para obter padrões de API ✅
- [x] **OBRIGATÓRIO**: Executar `mvp24h_infrastructure_guide` com topic `webapi-advanced` para recursos avançados ✅
- [x] **OBRIGATÓRIO**: Executar `mvp24h_modernization_guide` com category `apis` e feature `problem-details` para tratamento de erros ✅
- [x] Analisar classes base fornecidas pelo Mvp24Hours (ex: `Mvp24HoursController`, `ApiControllerBase`)
- [x] Verificar helpers e extensions para resposta HTTP
- [x] Identificar padrões de validação e tratamento de erros fornecidos pelo framework

**📝 Insights obtidos:**

**1. Configuração de WebAPI Essencial:**
```csharp
// Program.cs
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursMapService(assemblyMap: Assembly.GetExecutingAssembly());
builder.Services.AddMvp24HoursWebJson();
builder.Services.AddMvp24HoursWebExceptions(options => { });
builder.Services.AddMvp24HoursWebCors(options => { });
```

**2. Native OpenAPI (.NET 9) - Implementação Direta:**
Baseado no aprendizado documentado em `tasks-001-learning.md`, usar implementação direta:
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
        return Task.CompletedTask;
    });
});

// Pipeline
app.MapOpenApi("/openapi/{documentName}.json");
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "DesafioComIA API v1.0.0");
    options.RoutePrefix = "swagger";
});
```
**Nota:** O helper `AddMvp24HoursNativeOpenApi` apresentou problemas (bug no `MapMvp24HoursNativeOpenApi`).

**3. ProblemDetails (RFC 7807):**
O middleware customizado já implementado funciona corretamente. Mantém extração de `ValidationErrors` do Mvp24Hours via reflection.

**4. Mapeamento de Exceções de Domínio:**
Implementar middleware customizado para mapear:
- `ClienteNaoEncontradoException` → 404 Not Found
- `ClienteJaExisteException` → 409 Conflict
- `ValidationException` (Mvp24Hours) → 400 Bad Request
- `BusinessException` → 400 Bad Request

**5. Correlation ID:**
```csharp
app.UseMvp24HoursCorrelationId();
```

**6. Rate Limiting (Opcional):**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
    });
});
app.UseRateLimiter();
```

**7. Security Headers (Recomendado):**
```csharp
builder.Services.AddMvp24HoursSecurityHeaders(options =>
{
    options.AddContentSecurityPolicy = true;
    options.AddXContentTypeOptions = true;
    options.AddXFrameOptions = true;
    options.RemoveServerHeader = true;
});
app.UseMvp24HoursSecurityHeaders();
```

**8. Controllers - Padrão Obrigatório:**
- Usar `ISender` do Mvp24Hours para CQRS
- Usar `[ApiController]` para validação automática
- Usar `[Produces("application/json")]`
- Não injetar repositórios diretamente
- Sempre usar `CancellationToken`
- Sempre ter XML comments para Swagger
- Sempre ter `[ProducesResponseType]` para todos status codes

**9. XML Comments para Swagger:**
Configurar no `.csproj`:
```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

**🎯 Decisões para implementação:**
1. Usar **Native OpenAPI** (.NET 9) com implementação direta
2. Manter **middleware customizado** de ProblemDetails (já funciona)
3. Implementar **Correlation ID** para rastreabilidade
4. Adicionar **Security Headers** básicos
5. Considerar **Rate Limiting** (nativo .NET) para proteção da API
6. Controllers devem seguir padrões CQRS com `ISender` (Mvp24Hours)
7. Documentação Swagger completa com XML comments

#### W1.3: Criar GetClienteByIdQuery ✅
- [x] Criar pasta `Queries/Cliente` no projeto Application (se não existir)
- [x] Criar `GetClienteByIdQuery` implementando `IMediatorQuery<ClienteDto>` do Mvp24Hours
- [x] Adicionar propriedade:
  - `Id` (Guid, required)
- [x] Usar `record` para imutabilidade

**📄 Arquivo criado:** `src/DesafioComIA.Application/Queries/Cliente/GetClienteByIdQuery.cs`

#### W1.4: Criar GetClienteByIdQueryValidator ✅
- [x] Criar `GetClienteByIdQueryValidator` herdando de `AbstractValidator<GetClienteByIdQuery>`
- [x] Implementar regras de validação:
  - `Id`: Não pode ser Guid.Empty
  - `Id`: Mensagem de erro personalizada em português

**📄 Arquivo criado:** `src/DesafioComIA.Application/Queries/Cliente/GetClienteByIdQueryValidator.cs`

#### W1.5: Criar GetClienteByIdQueryHandler ✅
- [x] Criar `GetClienteByIdQueryHandler` implementando `IMediatorQueryHandler<GetClienteByIdQuery, ClienteDto>` do Mvp24Hours
- [x] Injetar dependências:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IMapper`
- [x] Implementar método `Handle`:
  - Buscar cliente por Id usando `GetByIdAsync` do repositório
  - Se não encontrado, lançar `ClienteNaoEncontradoException`
  - Mapear para `ClienteDto` e retornar

**📄 Arquivo criado:** `src/DesafioComIA.Application/Queries/Cliente/GetClienteByIdQueryHandler.cs`

#### W1.6: Criar UpdateClienteCommand (PUT) ✅
- [x] Criar pasta `Commands/Cliente` no projeto Application (se não existir)
- [x] Criar `UpdateClienteCommand` implementando `IMediatorCommand<ClienteDto>` do Mvp24Hours
- [x] Adicionar propriedades:
  - `Id` (Guid, required)
  - `Nome` (string, required)
  - `Cpf` (string, required)
  - `Email` (string, required)
- [x] Usar `record` para imutabilidade

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/UpdateClienteCommand.cs`

#### W1.7: Criar UpdateClienteCommandValidator ✅
- [x] Criar `UpdateClienteCommandValidator` herdando de `AbstractValidator<UpdateClienteCommand>`
- [x] Implementar regras de validação:
  - `Id`: Não pode ser Guid.Empty
  - `Nome`: Não vazio, mínimo 3 caracteres, máximo 200 caracteres
  - `Cpf`: Não vazio, usar validação do ValueObject `Cpf` do Mvp24Hours
  - `Email`: Não vazio, usar validação do ValueObject `Email` do Mvp24Hours
- [x] Adicionar mensagens de erro personalizadas em português

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/UpdateClienteCommandValidator.cs`

#### W1.8: Criar UpdateClienteCommandHandler ✅
- [x] Criar `UpdateClienteCommandHandler` implementando `IMediatorCommandHandler<UpdateClienteCommand, ClienteDto>` do Mvp24Hours
- [x] Injetar dependências:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IUnitOfWorkAsync` do Mvp24Hours
  - `IMapper`
- [x] Implementar método `Handle`:
  - Buscar cliente existente por Id
  - Se não encontrado, lançar `ClienteNaoEncontradoException`
  - Criar instância de `Cpf` ValueObject (Mvp24Hours) a partir da string do comando
  - Criar instância de `Email` ValueObject (Mvp24Hours) a partir da string do comando
  - Validar se novo CPF já existe em outro cliente
  - Validar se novo Email já existe em outro cliente
  - Atualizar todas as propriedades do cliente (Nome, Cpf, Email)
  - Salvar mudanças com UnitOfWork
  - Mapear para DTO e retornar

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/UpdateClienteCommandHandler.cs`

#### W1.9: Criar PatchClienteCommand (PATCH) ✅
- [x] Criar `PatchClienteCommand` implementando `IMediatorCommand<ClienteDto>` do Mvp24Hours
- [x] Adicionar propriedades opcionais:
  - `Id` (Guid, required)
  - `Nome` (string?, optional)
  - `Cpf` (string?, optional)
  - `Email` (string?, optional)
- [x] Usar `record` para imutabilidade

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/PatchClienteCommand.cs`

#### W1.10: Criar PatchClienteCommandValidator ✅
- [x] Criar `PatchClienteCommandValidator` herdando de `AbstractValidator<PatchClienteCommand>`
- [x] Implementar regras de validação:
  - `Id`: Não pode ser Guid.Empty
  - `Nome`: Se informado, mínimo 3 caracteres, máximo 200 caracteres
  - `Cpf`: Se informado, deve ser válido usando ValueObject `Cpf` do Mvp24Hours
  - `Email`: Se informado, deve ser válido usando ValueObject `Email` do Mvp24Hours
  - Pelo menos um campo (Nome, Cpf ou Email) deve ser informado
- [x] Adicionar mensagens de erro personalizadas em português

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/PatchClienteCommandValidator.cs`

#### W1.11: Criar PatchClienteCommandHandler ✅
- [x] Criar `PatchClienteCommandHandler` implementando `IMediatorCommandHandler<PatchClienteCommand, ClienteDto>` do Mvp24Hours
- [x] Injetar dependências:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IUnitOfWorkAsync` do Mvp24Hours
  - `IMapper`
- [x] Implementar método `Handle`:
  - Buscar cliente existente por Id
  - Se não encontrado, lançar `ClienteNaoEncontradoException`
  - Se `Nome` informado, atualizar Nome
  - Se `Cpf` informado, criar ValueObject `Cpf` (Mvp24Hours), validar unicidade e atualizar
  - Se `Email` informado, criar ValueObject `Email` (Mvp24Hours), validar unicidade e atualizar
  - Salvar mudanças com UnitOfWork
  - Mapear para DTO e retornar

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/PatchClienteCommandHandler.cs`

#### W1.12: Criar DeleteClienteCommand ✅
- [x] Criar `DeleteClienteCommand` implementando `IMediatorCommand<bool>` do Mvp24Hours
- [x] Adicionar propriedade:
  - `Id` (Guid, required)
- [x] Usar `record` para imutabilidade

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/DeleteClienteCommand.cs`

#### W1.13: Criar DeleteClienteCommandValidator ✅
- [x] Criar `DeleteClienteCommandValidator` herdando de `AbstractValidator<DeleteClienteCommand>`
- [x] Implementar regras de validação:
  - `Id`: Não pode ser Guid.Empty
- [x] Adicionar mensagem de erro personalizada em português

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/DeleteClienteCommandValidator.cs`

#### W1.14: Criar DeleteClienteCommandHandler ✅
- [x] Criar `DeleteClienteCommandHandler` implementando `IMediatorCommandHandler<DeleteClienteCommand, bool>` do Mvp24Hours
- [x] Injetar dependências:
  - `IRepositoryAsync<Cliente>` do Mvp24Hours
  - `IUnitOfWorkAsync` do Mvp24Hours
- [x] Implementar método `Handle`:
  - Buscar cliente existente por Id
  - Se não encontrado, lançar `ClienteNaoEncontradoException`
  - Remover cliente usando `RemoveAsync` do repositório
  - Salvar mudanças com UnitOfWork
  - Retornar `true` indicando sucesso

**📄 Arquivo criado:** `src/DesafioComIA.Application/Commands/Cliente/DeleteClienteCommandHandler.cs`

#### W1.15: Implementar Novos Endpoints no ClientesController ✅
- [x] Adicionar endpoint `GET /api/clientes/{id}`:
  - Receber `id` como parâmetro de rota (Guid)
  - Criar `GetClienteByIdQuery` com o id
  - Enviar query via `ISender.SendAsync()`
  - Retornar `200 OK` com `ClienteDto` no body
  - Retornar `404 Not Found` se cliente não existir
- [x] Adicionar endpoint `PUT /api/clientes/{id}`:
  - Receber `id` como parâmetro de rota (Guid)
  - Receber dados do cliente no body
  - Criar `UpdateClienteCommand` com id e dados
  - Enviar comando via `ISender.SendAsync()`
  - Retornar `200 OK` com `ClienteDto` atualizado
  - Retornar `404 Not Found` se cliente não existir
  - Retornar `409 Conflict` se CPF/Email já existir
- [x] Adicionar endpoint `PATCH /api/clientes/{id}`:
  - Receber `id` como parâmetro de rota (Guid)
  - Receber dados parciais no body
  - Criar `PatchClienteCommand` com id e dados parciais
  - Enviar comando via `ISender.SendAsync()`
  - Retornar `200 OK` com `ClienteDto` atualizado
  - Retornar `404 Not Found` se cliente não existir
  - Retornar `409 Conflict` se CPF/Email já existir
- [x] Adicionar endpoint `DELETE /api/clientes/{id}`:
  - Receber `id` como parâmetro de rota (Guid)
  - Criar `DeleteClienteCommand` com o id
  - Enviar comando via `ISender.SendAsync()`
  - Retornar `204 No Content` em caso de sucesso
  - Retornar `404 Not Found` se cliente não existir

**📄 Arquivo atualizado:** `src/DesafioComIA.Api/Controllers/ClientesController.cs`

**📄 DTOs criados:**
- `src/DesafioComIA.Application/DTOs/UpdateClienteDto.cs`
- `src/DesafioComIA.Application/DTOs/PatchClienteDto.cs`

**📄 Entidade atualizada:** `src/DesafioComIA.Domain/Entities/Cliente.cs`
- Adicionados métodos: `AtualizarNome()`, `AtualizarCpf()`, `AtualizarEmail()`

#### W1.16: Adicionar Location Header no POST ✅
- [x] Atualizar endpoint `POST /api/clientes`:
  - Após criar cliente, retornar `201 Created`
  - Adicionar header `Location` com URL do recurso criado: `/api/clientes/{id}`
  - Usar `CreatedAtAction` ou `CreatedAtRoute` do ASP.NET Core

**Nota:** Já implementado anteriormente usando `CreatedAtAction(nameof(GetById), new { id = result.Id }, result)`

#### W1.17: Configurar Tratamento de Erros com ProblemDetails ✅
- [x] **OBRIGATÓRIO**: Consultar `mvp24h_modernization_guide` com category `apis` e feature `problem-details`
- [x] Configurar middleware de exception handling para retornar ProblemDetails
- [x] Mapear exceções para status codes apropriados:
  - `ClienteNaoEncontradoException` → 404 Not Found
  - `ClienteJaExisteException` → 409 Conflict
  - `ValidationException` (FluentValidation) → 400 Bad Request
  - Exceções não tratadas → 500 Internal Server Error
- [x] Garantir que todos os erros retornem formato ProblemDetails consistente

**Nota:** Já implementado anteriormente no `ExceptionHandlingMiddleware.cs`

#### W1.18: Atualizar Documentação Swagger/OpenAPI ✅
- [x] **OBRIGATÓRIO**: Consultar `mvp24h_reference_guide` com topic `documentation`
- [x] Adicionar `[ProducesResponseType]` em todos os endpoints:
  - `GET /api/clientes/{id}`: 200, 400, 404, 500
  - `PUT /api/clientes/{id}`: 200, 400, 404, 409, 500
  - `PATCH /api/clientes/{id}`: 200, 400, 404, 409, 500
  - `DELETE /api/clientes/{id}`: 204, 400, 404, 500
  - `POST /api/clientes`: 201, 400, 409, 500
- [x] Adicionar comentários XML para documentação:
  - Descrição de cada endpoint
  - Descrição de parâmetros
  - Exemplos de requisição/resposta
- [x] Configurar exemplos de ProblemDetails no Swagger

**Nota:** Todos os endpoints documentados com XML comments e `[ProducesResponseType]`

#### W1.19: Validação da Implementação RESTful ✅
- [x] Validar que todas as rotas seguem padrão RESTful:
  - Plural para recursos (`/clientes`) ✅
  - Métodos HTTP corretos (GET, POST, PUT, PATCH, DELETE) ✅
  - Códigos de status HTTP apropriados ✅
  - Headers corretos (Location, Content-Type) ✅
- [x] Validar idempotência:
  - PUT deve ser idempotente (mesma requisição múltiplas vezes = mesmo resultado) ✅
  - PATCH deve ser idempotente ✅
  - DELETE deve ser idempotente ✅
  - GET deve ser idempotente e seguro (sem efeitos colaterais) ✅
- [x] Validar semântica REST:
  - POST cria novo recurso (201 Created + Location header) ✅
  - PUT substitui recurso completamente (200 OK) ✅
  - PATCH atualiza parcialmente (200 OK) ✅
  - DELETE remove recurso (204 No Content) ✅
  - GET recupera recurso(s) (200 OK) ✅

**📊 Resumo dos Endpoints RESTful Implementados:**
| Método | Rota | Descrição | Status Code |
|--------|------|-----------|-------------|
| POST | /api/clientes | Criar cliente | 201 Created |
| GET | /api/clientes | Listar clientes | 200 OK |
| GET | /api/clientes/search | Buscar clientes | 200 OK |
| GET | /api/clientes/{id} | Obter cliente | 200 OK |
| PUT | /api/clientes/{id} | Atualizar completo | 200 OK |
| PATCH | /api/clientes/{id} | Atualizar parcial | 200 OK |
| DELETE | /api/clientes/{id} | Remover cliente | 204 No Content |

---

## 🌊 Wave 2: Implementação de Cache (TAR-008)

### Objetivo
Implementar estratégia de cache para otimizar performance das operações de listagem e busca de clientes, reduzindo carga no banco de dados.

### Pré-requisito: Consultar Mvp24Hours
**⚠️ IMPORTANTE**: Antes de implementar cache, SEMPRE consulte as ferramentas do Mvp24Hours:
- `mvp24h_modernization_guide` com category `caching` e feature `hybrid-cache` para cache moderno do .NET 9
- `mvp24h_infrastructure_guide` com topic `caching` para padrões de cache do Mvp24Hours
- `mvp24h_infrastructure_guide` com topic `caching-advanced` para estratégias avançadas
- `mvp24h_infrastructure_guide` com topic `caching-redis` para integração com Redis
- `mvp24h_database_advisor` para validar integração de cache com Repository/UnitOfWork

### Microtarefas

#### W2.1: Analisar Tecnologias de Cache Disponíveis ✅
- [x] **OBRIGATÓRIO**: Executar `mvp24h_modernization_guide` com category `caching` e feature `hybrid-cache`
- [x] **OBRIGATÓRIO**: Executar `mvp24h_infrastructure_guide` com topic `caching`
- [x] **OBRIGATÓRIO**: Executar `mvp24h_infrastructure_guide` com topic `caching-redis`
- [x] Avaliar opções de cache:
  - **HybridCache** (.NET 9) - Recomendado para cache em memória + distribuído ✅ ESCOLHIDO
  - **Redis** via Mvp24Hours - Para cache distribuído puro
  - **IMemoryCache** - Para cache em memória simples
- [x] Escolher tecnologia baseado em requisitos:
  - Se aplicação distribuída: Redis ou HybridCache com Redis
  - Se aplicação single-instance: HybridCache com memória ou IMemoryCache
  - Recomendação: **HybridCache** por ser nativo do .NET 9

**📝 Decisão**: Escolhido **HybridCache** do .NET 9 com Redis como L2 (opcional)

#### W2.2: Configurar HybridCache (.NET 9) ✅
- [x] **OBRIGATÓRIO**: Consultar `mvp24h_modernization_guide` com category `caching` e feature `hybrid-cache` antes de implementar
- [x] Instalar pacote NuGet (se não instalado):
  - `Microsoft.Extensions.Caching.Hybrid` (versão 9.3.0)
  - `Microsoft.Extensions.Caching.StackExchangeRedis` (versão 10.0.1)
- [x] Configurar HybridCache no `Program.cs`
- [x] Configurar Redis como backend (opcional, para cache distribuído)

**📄 Arquivos atualizados:**
- `src/DesafioComIA.Api/DesafioComIA.Api.csproj`
- `src/DesafioComIA.Infrastructure/DesafioComIA.Infrastructure.csproj`
- `src/DesafioComIA.Api/Program.cs`

#### W2.3: Criar Configuração de Cache em appsettings.json ✅
- [x] Adicionar seção de configuração de cache no `appsettings.json`
- [x] Criar classe de configuração `CacheSettings`
- [x] Registrar `CacheSettings` no DI

**📄 Arquivos criados/atualizados:**
- `src/DesafioComIA.Api/appsettings.json`
- `src/DesafioComIA.Infrastructure/Configuration/CacheSettings.cs`

#### W2.4: Criar Interface ICacheService ✅
- [x] Criar pasta `Services/Cache` no projeto Infrastructure
- [x] Criar interface `ICacheService` com métodos:
  - `GetOrCreateAsync<T>` - Busca ou cria valor no cache
  - `GetAsync<T>` - Busca valor no cache
  - `SetAsync<T>` - Define valor no cache
  - `RemoveAsync` - Remove chave(s) do cache
  - `RemoveByPatternAsync` - Remove chaves por padrão

**📄 Arquivo criado:** `src/DesafioComIA.Infrastructure/Services/Cache/ICacheService.cs`

#### W2.5: Implementar HybridCacheService ✅
- [x] **OBRIGATÓRIO**: Consultar documentação do HybridCache via `mvp24h_modernization_guide` antes de implementar
- [x] Criar `HybridCacheService` no projeto Infrastructure implementando `ICacheService`
- [x] Injetar dependências: `HybridCache`, `IOptions<CacheSettings>`, `ILogger`, `IConnectionMultiplexer?`
- [x] Implementar todos os métodos com tratamento de exceções e logging
- [x] Implementar rastreamento de chaves para invalidação por padrão

**📄 Arquivo criado:** `src/DesafioComIA.Infrastructure/Caching/HybridCacheService.cs`

#### W2.6: Registrar Cache Service no DI ✅
- [x] Registrado `ICacheService` como `HybridCacheService` no `Program.cs`
- [x] Configurado `IConnectionMultiplexer` para Redis (opcional)

**📄 Arquivo atualizado:** `src/DesafioComIA.Api/Program.cs`

#### W2.7: Criar Helper para Geração de Chaves de Cache ✅
- [x] Criar classe `CacheKeyHelper` no projeto Infrastructure
- [x] Criar métodos estáticos para gerar chaves consistentes:
  - `GetListClientesKey()` - Para listagens paginadas
  - `GetSearchClientesKey()` - Para buscas com filtros (usa hash MD5)
  - `GetClienteByIdKey()` - Para cliente específico
  - `GetClientesListPattern()` - Padrão para invalidar listagens
  - `GetClientesSearchPattern()` - Padrão para invalidar buscas
  - `GetClientesPattern()` - Padrão para invalidar tudo

**📄 Arquivo criado:** `src/DesafioComIA.Infrastructure/Services/Cache/CacheKeyHelper.cs`

#### W2.8: Implementar Cache em ListClientesQueryHandler ✅
- [x] Injetar `ICacheService` e `IOptions<CacheSettings>` no handler
- [x] Usar `GetOrCreateAsync` para buscar/criar cache automaticamente
- [x] TTL configurado via `CacheSettings.ListClientesTTLMinutes`

**📄 Arquivo atualizado:** `src/DesafioComIA.Application/Queries/Cliente/ListClientesQueryHandler.cs`

#### W2.9: Implementar Cache em GetClientesQueryHandler (Search) ✅
- [x] Injetar `ICacheService` no `GetClientesQueryHandler`
- [x] Injetar `IOptions<CacheSettings>`
- [x] No método `Handle`, antes de consultar banco:
  - Verificar se cache está habilitado
  - Gerar chave de cache usando `CacheKeyHelper.GetSearchClientesKey`
  - Usar `GetOrCreateAsync` para buscar/criar cache automaticamente
- [x] Após consultar banco de dados:
  - Armazenar resultado no cache usando `GetOrCreateAsync`
  - Usar TTL configurado em `CacheSettings.SearchClientesTTLMinutes`
  - Retornar resultado

**📄 Arquivo atualizado:** `src/DesafioComIA.Application/Queries/Cliente/GetClientesQueryHandler.cs`

#### W2.10: Implementar Cache em GetClienteByIdQueryHandler ✅
- [x] Injetar `ICacheService` e `IOptions<CacheSettings>` no handler
- [x] Usar `GetOrCreateAsync` para buscar/criar cache automaticamente
- [x] TTL configurado via `CacheSettings.GetClienteByIdTTLMinutes`

**📄 Arquivo atualizado:** `src/DesafioComIA.Application/Queries/Cliente/GetClienteByIdQueryHandler.cs`

#### W2.11: Implementar Invalidação de Cache em CreateClienteCommandHandler ✅
- [x] Injetar `ICacheService` no `CreateClienteCommandHandler`
- [x] Após salvar cliente com sucesso:
  - Invalidar cache de listagem usando `RemoveByPatternAsync` com padrão `"clientes:list:*"`
  - Invalidar cache de busca usando `RemoveByPatternAsync` com padrão `"clientes:search:*"`
  - Fazer log da invalidação
- [x] Garantir que invalidação não afete o sucesso da operação:
  - Usar try-catch para evitar que falha no cache invalide operação
  - Fazer log de erro se invalidação falhar

**📄 Arquivo atualizado:** `src/DesafioComIA.Application/Commands/Cliente/CreateClienteCommandHandler.cs`

#### W2.12: Implementar Invalidação de Cache em UpdateClienteCommandHandler ✅
- [x] Injetar `ICacheService` no handler
- [x] Invalidar cache específico do cliente, listagem e busca após atualização
- [x] Tratamento de exceções para não afetar a operação principal

**📄 Arquivo atualizado:** `src/DesafioComIA.Application/Commands/Cliente/UpdateClienteCommandHandler.cs`

#### W2.13: Implementar Invalidação de Cache em PatchClienteCommandHandler ✅
- [x] Injetar `ICacheService` no `PatchClienteCommandHandler`
- [x] Após atualizar cliente parcialmente com sucesso:
  - Invalidar cache específico do cliente usando `RemoveAsync` com chave `GetClienteByIdKey(id)`
  - Invalidar cache de listagem usando `RemoveByPatternAsync` com padrão `"clientes:list:*"`
  - Invalidar cache de busca usando `RemoveByPatternAsync` com padrão `"clientes:search:*"`
  - Fazer log da invalidação
- [x] Garantir que invalidação não afete o sucesso da operação

**📄 Arquivo atualizado:** `src/DesafioComIA.Application/Commands/Cliente/PatchClienteCommandHandler.cs`

#### W2.14: Implementar Invalidação de Cache em DeleteClienteCommandHandler ✅
- [x] Injetar `ICacheService` no handler
- [x] Invalidar cache específico do cliente, listagem e busca após deleção
- [x] Tratamento de exceções para não afetar a operação principal

**📄 Arquivo atualizado:** `src/DesafioComIA.Application/Commands/Cliente/DeleteClienteCommandHandler.cs`

#### W2.15: Adicionar Redis ao docker-compose.yml ✅
- [x] Atualizar `docker-compose.yml` adicionando serviço Redis:
  ```yaml
  redis:
    image: redis:7-alpine
    container_name: desafio_redis
    restart: always
    ports:
      - "6379:6379"
    volumes:
      - ./data/redis:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5
    command: redis-server --appendonly yes
  ```
- [x] Adicionar pasta `data/redis/` ao `.gitignore` (já coberto por `data/`)
- [x] Atualizar README.md com instruções de uso do Redis

**📄 Arquivo atualizado:** `docker-compose.yml`

#### W2.16: Criar Endpoint de Diagnóstico de Cache ✅
- [x] Criar `CacheController` no projeto API
- [x] Endpoint `GET /api/cache/stats` - Retorna configurações e status Redis
- [x] Endpoint `DELETE /api/cache/clear` - Limpa todo cache de clientes
- [x] Endpoint `DELETE /api/cache/key/{key}` - Remove chave específica
- [x] Todos endpoints restritos a ambiente Development

**📄 Arquivo criado:** `src/DesafioComIA.Api/Controllers/CacheController.cs`

#### W2.17: Validação da Implementação de Cache ✅
- [x] Validar que cache está funcionando:
  - Build da solução: ✅ Sucesso (6 warnings de nullability - menores)
  - Todos os testes passando: ✅ 32/32 testes aprovados
  - Implementação de GetOrCreateAsync em todos os handlers de query
- [x] Validar invalidação de cache:
  - Criar cliente invalida cache de listagem e busca ✅
  - Atualizar cliente (PUT/PATCH) invalida cache do cliente e listagem ✅
  - Remover cliente invalida cache do cliente e listagem ✅
- [x] Validar comportamento em caso de falha:
  - Try-catch em todas as operações de cache
  - Erros são logados mas não interrompem a operação principal
  - Testes executam com cache desabilitado para isolamento ✅
- [x] README.md atualizado com instruções de Redis e cache

---

## 🌊 Wave 3: Implementação de Telemetria com OpenTelemetry (TAR-009)

### Objetivo
Implementar observabilidade completa da API utilizando OpenTelemetry para logs, traces e métricas, com exportação via OTLP para Jaeger, Prometheus e Grafana.

### Pré-requisito: Consultar Mvp24Hours
**⚠️ IMPORTANTE**: Antes de implementar observabilidade, SEMPRE consulte as ferramentas do Mvp24Hours:
- `mvp24h_observability_setup` com component `overview` para visão geral
- `mvp24h_observability_setup` com component `logging` para configuração de logs
- `mvp24h_observability_setup` com component `tracing` para configuração de traces
- `mvp24h_observability_setup` com component `metrics` para configuração de métricas
- `mvp24h_observability_setup` com component `exporters` para configuração de exportadores
- `mvp24h_cqrs_guide` com topic `cqrs-tracing` para instrumentação de CQRS
- `mvp24h_cqrs_guide` com topic `cqrs-telemetry` para telemetria em Commands/Queries

### Microtarefas

#### W3.1: Analisar Requisitos de Observabilidade
- [ ] **OBRIGATÓRIO**: Executar `mvp24h_observability_setup` com component `overview`
- [ ] **OBRIGATÓRIO**: Executar `mvp24h_cqrs_guide` com topic `cqrs-tracing`
- [ ] **OBRIGATÓRIO**: Executar `mvp24h_cqrs_guide` com topic `cqrs-telemetry`
- [ ] Identificar componentes de observabilidade necessários:
  - **Logs**: Estruturados em JSON com correlation ID
  - **Traces**: Rastreamento de requisições HTTP e operações CQRS
  - **Métricas**: Performance, negócio e recursos
- [ ] Identificar ferramentas de visualização:
  - **Jaeger**: Visualização de traces
  - **Prometheus**: Coleta e armazenamento de métricas
  - **Grafana**: Dashboards e visualização unificada

#### W3.2: Instalar Pacotes NuGet - OpenTelemetry
- [ ] **OBRIGATÓRIO**: Consultar `mvp24h_observability_setup` com component `exporters` antes de instalar
- [ ] Instalar pacotes core:
  - `OpenTelemetry` (versão 1.*)
  - `OpenTelemetry.Extensions.Hosting` (versão 1.*)
  - `OpenTelemetry.Instrumentation.AspNetCore` (versão 1.*)
  - `OpenTelemetry.Instrumentation.Http` (versão 1.*)
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` (versão 1.*)
- [ ] Instalar exportadores:
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol` (versão 1.*) - OTLP
  - `OpenTelemetry.Exporter.Console` (versão 1.*) - Console (Development)
  - `OpenTelemetry.Exporter.Prometheus.AspNetCore` (versão 1.*) - Prometheus
- [ ] Instalar integração com logging:
  - `OpenTelemetry.Extensions.Logging` (versão 1.*)

#### W3.3: Configurar OpenTelemetry em appsettings.json
- [ ] Adicionar seção de configuração:
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
      "Jaeger": {
        "Endpoint": "http://localhost:4318/v1/traces"
      },
      "Prometheus": {
        "Endpoint": "/metrics",
        "Port": 9464
      },
      "Tracing": {
        "Enabled": true,
        "SamplingProbability": 1.0
      },
      "Metrics": {
        "Enabled": true
      },
      "Logging": {
        "Enabled": true,
        "IncludeFormattedMessage": true,
        "IncludeScopes": true
      }
    }
  }
  ```

#### W3.4: Criar Classe de Configuração OpenTelemetrySettings
- [ ] Criar `OpenTelemetrySettings` no projeto API
- [ ] Adicionar propriedades:
  - `ServiceName` (string)
  - `ServiceVersion` (string)
  - `EnableConsoleExporter` (bool)
  - Classes aninhadas: `OtlpSettings`, `JaegerSettings`, `PrometheusSettings`, `TracingSettings`, `MetricsSettings`, `LoggingSettings`
- [ ] Registrar no DI:
  ```csharp
  builder.Services.Configure<OpenTelemetrySettings>(
      builder.Configuration.GetSection("OpenTelemetry"));
  ```

#### W3.5: Configurar OpenTelemetry - Tracing
- [ ] **OBRIGATÓRIO**: Consultar `mvp24h_observability_setup` com component `tracing` antes de implementar
- [ ] Adicionar no `Program.cs`:
  ```csharp
  builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource
          .AddService(serviceName: "DesafioComIA.Api", serviceVersion: "1.0.0"))
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation(options =>
          {
              options.RecordException = true;
              options.EnrichWithHttpRequest = (activity, request) =>
              {
                  activity.SetTag("http.request.method", request.Method);
                  activity.SetTag("http.request.path", request.Path);
              };
              options.EnrichWithHttpResponse = (activity, response) =>
              {
                  activity.SetTag("http.response.status_code", response.StatusCode);
              };
          })
          .AddHttpClientInstrumentation()
          .AddEntityFrameworkCoreInstrumentation(options =>
          {
              options.SetDbStatementForText = true;
              options.SetDbStatementForStoredProcedure = true;
          })
          .AddSource("DesafioComIA.*")
          .AddOtlpExporter(options =>
          {
              options.Endpoint = new Uri("http://localhost:4317");
              options.Protocol = OtlpExportProtocol.Grpc;
          })
          .AddConsoleExporter());
  ```

#### W3.6: Configurar OpenTelemetry - Metrics
- [ ] **OBRIGATÓRIO**: Consultar `mvp24h_observability_setup` com component `metrics` antes de implementar
- [ ] Adicionar no `Program.cs` (continuação do AddOpenTelemetry):
  ```csharp
  .WithMetrics(metrics => metrics
      .AddAspNetCoreInstrumentation()
      .AddHttpClientInstrumentation()
      .AddRuntimeInstrumentation()
      .AddProcessInstrumentation()
      .AddMeter("DesafioComIA.*")
      .AddOtlpExporter(options =>
      {
          options.Endpoint = new Uri("http://localhost:4317");
          options.Protocol = OtlpExportProtocol.Grpc;
      })
      .AddPrometheusExporter()
      .AddConsoleExporter());
  ```
- [ ] Configurar endpoint Prometheus:
  ```csharp
  app.MapPrometheusScrapingEndpoint("/metrics");
  ```

#### W3.7: Configurar OpenTelemetry - Logging
- [ ] **OBRIGATÓRIO**: Consultar `mvp24h_observability_setup` com component `logging` antes de implementar
- [ ] Configurar logging estruturado no `Program.cs`:
  ```csharp
  builder.Logging.ClearProviders();
  builder.Logging.AddOpenTelemetry(options =>
  {
      options.IncludeFormattedMessage = true;
      options.IncludeScopes = true;
      options.ParseStateValues = true;
      options.AddOtlpExporter(otlp =>
      {
          otlp.Endpoint = new Uri("http://localhost:4317");
          otlp.Protocol = OtlpExportProtocol.Grpc;
      });
      options.AddConsoleExporter();
  });
  builder.Logging.AddJsonConsole(options =>
  {
      options.IncludeScopes = true;
      options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
      options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
      {
          Indented = false
      };
  });
  ```

#### W3.8: Criar ActivitySource para Instrumentação Manual
- [ ] Criar classe `Telemetry` no projeto Application:
  ```csharp
  public static class Telemetry
  {
      public const string ServiceName = "DesafioComIA.Api";
      public static readonly ActivitySource ActivitySource = new(ServiceName, "1.0.0");
  }
  ```
- [ ] Registrar ActivitySource no DI se necessário

#### W3.9: Criar Métricas Customizadas de Negócio
- [ ] Criar classe `ClienteMetrics` no projeto Application:
  ```csharp
  public class ClienteMetrics
  {
      private readonly Counter<long> _clientesCriados;
      private readonly Counter<long> _clientesAtualizados;
      private readonly Counter<long> _clientesRemovidos;
      private readonly Counter<long> _buscasRealizadas;
      private readonly Histogram<double> _tempoProcessamento;
      
      public ClienteMetrics(IMeterFactory meterFactory)
      {
          var meter = meterFactory.Create("DesafioComIA.Clientes");
          
          _clientesCriados = meter.CreateCounter<long>(
              "clientes.criados",
              description: "Total de clientes criados");
              
          _clientesAtualizados = meter.CreateCounter<long>(
              "clientes.atualizados",
              description: "Total de clientes atualizados");
              
          _clientesRemovidos = meter.CreateCounter<long>(
              "clientes.removidos",
              description: "Total de clientes removidos");
              
          _buscasRealizadas = meter.CreateCounter<long>(
              "clientes.buscas",
              description: "Total de buscas realizadas");
              
          _tempoProcessamento = meter.CreateHistogram<double>(
              "clientes.processamento.tempo",
              unit: "ms",
              description: "Tempo de processamento das operações");
      }
      
      public void ClienteCriado() => _clientesCriados.Add(1);
      public void ClienteAtualizado() => _clientesAtualizados.Add(1);
      public void ClienteRemovido() => _clientesRemovidos.Add(1);
      public void BuscaRealizada() => _buscasRealizadas.Add(1);
      public void RegistrarTempoProcessamento(double milliseconds) => 
          _tempoProcessamento.Record(milliseconds);
  }
  ```
- [ ] Registrar `ClienteMetrics` no DI como Singleton:
  ```csharp
  builder.Services.AddSingleton<ClienteMetrics>();
  ```

#### W3.10: Instrumentar CreateClienteCommandHandler com Tracing e Métricas
- [ ] **OBRIGATÓRIO**: Consultar `mvp24h_cqrs_guide` com topic `cqrs-tracing` antes de instrumentar
- [ ] Injetar `ClienteMetrics` no handler
- [ ] No método `Handle`, adicionar instrumentação:
  - Criar span manual usando `Telemetry.ActivitySource.StartActivity("CreateClienteCommand")`
  - Adicionar tags relevantes: `cliente.nome`, `cliente.cpf`, `cliente.email`
  - Registrar eventos importantes: validação, verificação de duplicidade, criação
  - Medir tempo de processamento
  - Incrementar métrica de clientes criados
  - Garantir que span seja finalizado (usar `using` ou `try-finally`)
- [ ] Exemplo de código:
  ```csharp
  using var activity = Telemetry.ActivitySource.StartActivity("CreateCliente");
  activity?.SetTag("cliente.nome", command.Nome);
  
  var stopwatch = Stopwatch.StartNew();
  try
  {
      // ... lógica existente ...
      _metrics.ClienteCriado();
      activity?.AddEvent(new("Cliente criado com sucesso"));
      return result;
  }
  catch (Exception ex)
  {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.RecordException(ex);
      throw;
  }
  finally
  {
      stopwatch.Stop();
      _metrics.RegistrarTempoProcessamento(stopwatch.ElapsedMilliseconds);
  }
  ```

#### W3.11: Instrumentar UpdateClienteCommandHandler
- [ ] Injetar `ClienteMetrics` no handler
- [ ] Adicionar instrumentação similar ao CreateClienteCommandHandler:
  - Criar span "UpdateCliente"
  - Adicionar tags: `cliente.id`, `cliente.nome`
  - Registrar eventos importantes
  - Incrementar métrica de clientes atualizados
  - Medir tempo de processamento

#### W3.12: Instrumentar PatchClienteCommandHandler
- [ ] Injetar `ClienteMetrics` no handler
- [ ] Adicionar instrumentação:
  - Criar span "PatchCliente"
  - Adicionar tags: `cliente.id`, campos atualizados
  - Registrar eventos importantes
  - Incrementar métrica de clientes atualizados
  - Medir tempo de processamento

#### W3.13: Instrumentar DeleteClienteCommandHandler
- [ ] Injetar `ClienteMetrics` no handler
- [ ] Adicionar instrumentação:
  - Criar span "DeleteCliente"
  - Adicionar tag: `cliente.id`
  - Registrar eventos importantes
  - Incrementar métrica de clientes removidos
  - Medir tempo de processamento

#### W3.14: Instrumentar Query Handlers com Tracing e Métricas
- [ ] Instrumentar `ListClientesQueryHandler`:
  - Criar span "ListClientes"
  - Adicionar tags: `page`, `pageSize`, `sortBy`
  - Registrar cache hit/miss como evento
  - Incrementar métrica de buscas realizadas
  - Medir tempo de processamento
- [ ] Instrumentar `GetClientesQueryHandler`:
  - Criar span "SearchClientes"
  - Adicionar tags: filtros aplicados
  - Registrar cache hit/miss como evento
  - Incrementar métrica de buscas realizadas
  - Medir tempo de processamento
- [ ] Instrumentar `GetClienteByIdQueryHandler`:
  - Criar span "GetClienteById"
  - Adicionar tag: `cliente.id`
  - Registrar cache hit/miss como evento
  - Incrementar métrica de buscas realizadas
  - Medir tempo de processamento

#### W3.15: Adicionar Métricas de Cache
- [ ] Criar `CacheMetrics` no projeto Infrastructure:
  ```csharp
  public class CacheMetrics
  {
      private readonly Counter<long> _cacheHits;
      private readonly Counter<long> _cacheMisses;
      private readonly Counter<long> _cacheInvalidations;
      
      public CacheMetrics(IMeterFactory meterFactory)
      {
          var meter = meterFactory.Create("DesafioComIA.Cache");
          
          _cacheHits = meter.CreateCounter<long>(
              "cache.hits",
              description: "Total de cache hits");
              
          _cacheMisses = meter.CreateCounter<long>(
              "cache.misses",
              description: "Total de cache misses");
              
          _cacheInvalidations = meter.CreateCounter<long>(
              "cache.invalidations",
              description: "Total de invalidações de cache");
      }
      
      public void CacheHit(string key) => _cacheHits.Add(1, new KeyValuePair<string, object?>("cache.key", key));
      public void CacheMiss(string key) => _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache.key", key));
      public void CacheInvalidation(string pattern) => _cacheInvalidations.Add(1, new KeyValuePair<string, object?>("cache.pattern", pattern));
  }
  ```
- [ ] Registrar `CacheMetrics` no DI
- [ ] Injetar `CacheMetrics` no `HybridCacheService`
- [ ] Registrar métricas em todas as operações de cache:
  - `GetAsync`: incrementar hit ou miss
  - `RemoveAsync` e `RemoveByPatternAsync`: incrementar invalidations

#### W3.16: Configurar Correlation ID e Context Propagation
- [ ] Criar middleware `CorrelationIdMiddleware`:
  - Gerar ou extrair correlation ID do header `X-Correlation-ID`
  - Adicionar correlation ID ao `Activity.Current`
  - Adicionar correlation ID ao `ILogger` scope
  - Adicionar correlation ID ao response header
- [ ] Registrar middleware no pipeline:
  ```csharp
  app.UseMiddleware<CorrelationIdMiddleware>();
  ```
- [ ] Garantir que correlation ID seja propagado em todos os logs e traces

#### W3.17: Configurar Mascaramento de Dados Sensíveis
- [ ] Criar `SensitiveDataProcessor` para remover/mascarar dados sensíveis:
  - CPF deve ser mascarado: `123.456.789-00` → `***.456.789-**`
  - Email deve ser mascarado: `user@example.com` → `u***@example.com`
- [ ] Aplicar mascaramento em:
  - Tags de Activity/Span
  - Logs estruturados
  - Mensagens de exceção
- [ ] Criar helper extension para Activity:
  ```csharp
  public static class ActivityExtensions
  {
      public static Activity? SetTagSafe(this Activity? activity, string key, string? value)
      {
          if (activity == null || value == null) return activity;
          
          if (key.Contains("cpf", StringComparison.OrdinalIgnoreCase))
              value = SensitiveDataProcessor.MaskCpf(value);
          else if (key.Contains("email", StringComparison.OrdinalIgnoreCase))
              value = SensitiveDataProcessor.MaskEmail(value);
          
          return activity.SetTag(key, value);
      }
  }
  ```

#### W3.18: Adicionar Jaeger, Prometheus e Grafana ao docker-compose.yml
- [ ] Adicionar serviço Jaeger:
  ```yaml
  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: desafio_jaeger
    restart: always
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "16686:16686" # Jaeger UI
    environment:
      - COLLECTOR_OTLP_ENABLED=true
  ```
- [ ] Adicionar serviço Prometheus:
  ```yaml
  prometheus:
    image: prom/prometheus:latest
    container_name: desafio_prometheus
    restart: always
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - ./data/prometheus:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
  ```
- [ ] Adicionar serviço Grafana:
  ```yaml
  grafana:
    image: grafana/grafana:latest
    container_name: desafio_grafana
    restart: always
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - ./data/grafana:/var/lib/grafana
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning
  ```

#### W3.19: Criar Arquivo de Configuração do Prometheus
- [ ] Criar pasta `monitoring/` na raiz do projeto
- [ ] Criar arquivo `monitoring/prometheus.yml`:
  ```yaml
  global:
    scrape_interval: 15s
    evaluation_interval: 15s
  
  scrape_configs:
    - job_name: 'desafio-api'
      static_configs:
        - targets: ['host.docker.internal:9464']
      metrics_path: '/metrics'
  ```

#### W3.20: Criar Dashboards do Grafana
- [ ] Criar pasta `monitoring/grafana/provisioning/datasources/`
- [ ] Criar arquivo `monitoring/grafana/provisioning/datasources/datasources.yml`:
  ```yaml
  apiVersion: 1
  
  datasources:
    - name: Prometheus
      type: prometheus
      access: proxy
      url: http://prometheus:9090
      isDefault: true
      editable: false
    
    - name: Jaeger
      type: jaeger
      access: proxy
      url: http://jaeger:16686
      editable: false
  ```
- [ ] Criar pasta `monitoring/grafana/provisioning/dashboards/`
- [ ] Criar arquivo `monitoring/grafana/provisioning/dashboards/dashboards.yml`:
  ```yaml
  apiVersion: 1
  
  providers:
    - name: 'Default'
      orgId: 1
      folder: ''
      type: file
      disableDeletion: false
      updateIntervalSeconds: 10
      allowUiUpdates: true
      options:
        path: /etc/grafana/provisioning/dashboards/definitions
  ```
- [ ] Criar pasta `monitoring/grafana/provisioning/dashboards/definitions/`
- [ ] Criar dashboard JSON básico `monitoring/grafana/provisioning/dashboards/definitions/api-overview.json`:
  - Painel: Taxa de requisições por endpoint
  - Painel: Tempo de resposta (percentis p50, p90, p99)
  - Painel: Taxa de erros por endpoint
  - Painel: Métricas de negócio (clientes criados, buscas, etc.)
  - Painel: Cache hit rate
  - Painel: Uso de recursos (CPU, memória)

#### W3.21: Atualizar README.md com Instruções de Observabilidade
- [ ] Adicionar seção "Observabilidade" no README.md
- [ ] Documentar como acessar ferramentas:
  - Jaeger UI: http://localhost:16686
  - Prometheus: http://localhost:9090
  - Grafana: http://localhost:3000 (admin/admin)
  - Métricas da API: http://localhost:9464/metrics
- [ ] Documentar métricas customizadas disponíveis
- [ ] Documentar como visualizar traces no Jaeger
- [ ] Documentar como criar queries no Prometheus
- [ ] Documentar dashboards disponíveis no Grafana

#### W3.22: Validação da Implementação de Observabilidade
- [ ] Validar Logs:
  - Logs estão em formato JSON estruturado
  - Correlation ID está presente em todos os logs
  - Logs contêm informações relevantes (timestamp, nível, mensagem, contexto)
  - Dados sensíveis estão mascarados
  - Logs aparecem no console e no Jaeger (via OTLP)
- [ ] Validar Traces:
  - Traces são criados para todas as requisições HTTP
  - Spans são criados para operações críticas (commands, queries, cache, DB)
  - Spans contêm atributos relevantes
  - Traces aparecem no Jaeger UI
  - Context propagation funciona corretamente
  - Exceções são capturadas nos traces
- [ ] Validar Métricas:
  - Métricas HTTP estão sendo coletadas
  - Métricas de negócio estão sendo coletadas
  - Métricas de cache estão sendo coletadas
  - Métricas aparecem no endpoint `/metrics`
  - Métricas são consumidas pelo Prometheus
  - Métricas aparecem no Grafana
- [ ] Validar Integração entre Ferramentas:
  - Jaeger recebe traces via OTLP
  - Prometheus coleta métricas via scraping
  - Grafana visualiza dados do Prometheus e Jaeger
  - Dashboards exibem informações corretamente

---

## 🌊 Wave 4: Testes de Integração

### Objetivo
Criar testes de integração abrangentes para validar todas as funcionalidades implementadas, incluindo rotas RESTful, cache e observabilidade.

### Microtarefas

#### W4.1: Configurar Projeto de Testes para Novas Funcionalidades
- [ ] Garantir que projeto de testes de integração está configurado (já existe de TAR-001 a TAR-006)
- [ ] Adicionar pacotes necessários para testar cache (se não instalados):
  - `Microsoft.Extensions.Caching.Memory`
  - `Moq` (para mock de ICacheService)
- [ ] Adicionar pacotes necessários para testar observabilidade:
  - `OpenTelemetry.Exporter.InMemory` (para capturar traces e métricas em testes)

#### W4.2: Criar Testes para GET /api/clientes/{id}
- [ ] Teste: Buscar cliente existente retorna 200 OK com dados corretos
- [ ] Teste: Buscar cliente inexistente retorna 404 Not Found
- [ ] Teste: Buscar com Id inválido (Guid.Empty) retorna 400 Bad Request
- [ ] Teste: Dados retornados estão completos (Id, Nome, Cpf, Email)
- [ ] Teste: CPF e Email estão no formato correto

#### W4.3: Criar Testes para PUT /api/clientes/{id}
- [ ] Teste: Atualizar cliente existente com dados válidos retorna 200 OK
- [ ] Teste: Atualizar cliente inexistente retorna 404 Not Found
- [ ] Teste: Atualizar com CPF duplicado (de outro cliente) retorna 409 Conflict
- [ ] Teste: Atualizar com Email duplicado (de outro cliente) retorna 409 Conflict
- [ ] Teste: Atualizar com Nome inválido retorna 400 Bad Request
- [ ] Teste: Atualizar com CPF inválido retorna 400 Bad Request
- [ ] Teste: Atualizar com Email inválido retorna 400 Bad Request
- [ ] Teste: Todas as propriedades são atualizadas corretamente
- [ ] Teste: PUT é idempotente (múltiplas requisições idênticas produzem mesmo resultado)

#### W4.4: Criar Testes para PATCH /api/clientes/{id}
- [ ] Teste: Atualizar apenas Nome retorna 200 OK com Nome atualizado
- [ ] Teste: Atualizar apenas CPF retorna 200 OK com CPF atualizado
- [ ] Teste: Atualizar apenas Email retorna 200 OK com Email atualizado
- [ ] Teste: Atualizar Nome e CPF retorna 200 OK com ambos atualizados
- [ ] Teste: Atualizar Nome e Email retorna 200 OK com ambos atualizados
- [ ] Teste: Atualizar CPF e Email retorna 200 OK com ambos atualizados
- [ ] Teste: Atualizar todos os campos retorna 200 OK com tudo atualizado
- [ ] Teste: PATCH sem nenhum campo retorna 400 Bad Request
- [ ] Teste: PATCH de cliente inexistente retorna 404 Not Found
- [ ] Teste: PATCH com CPF duplicado retorna 409 Conflict
- [ ] Teste: PATCH com Email duplicado retorna 409 Conflict
- [ ] Teste: PATCH é idempotente
- [ ] Teste: Campos não informados permanecem inalterados

#### W4.5: Criar Testes para DELETE /api/clientes/{id}
- [ ] Teste: Deletar cliente existente retorna 204 No Content
- [ ] Teste: Deletar cliente inexistente retorna 404 Not Found
- [ ] Teste: Cliente deletado não aparece em listagens
- [ ] Teste: Buscar cliente deletado retorna 404 Not Found
- [ ] Teste: DELETE é idempotente (segunda deleção retorna 404)
- [ ] Teste: Cliente é realmente removido do banco de dados

#### W4.6: Criar Testes para POST com Location Header
- [ ] Teste: POST retorna 201 Created (não 200 OK)
- [ ] Teste: Response contém header `Location`
- [ ] Teste: Location header contém URL do recurso criado (`/api/clientes/{id}`)
- [ ] Teste: GET na URL do Location retorna o cliente criado

#### W4.7: Criar Testes para Cache - Listagem
- [ ] Teste: Primeira listagem consulta banco de dados (cache miss)
- [ ] Teste: Segunda listagem idêntica retorna do cache (cache hit)
- [ ] Teste: Listagem com parâmetros diferentes não usa cache anterior
- [ ] Teste: Criar cliente invalida cache de listagem
- [ ] Teste: Próxima listagem após criação consulta banco novamente
- [ ] Teste: Cache expira após TTL configurado
- [ ] Teste: Desabilitar cache faz todas as requisições consultarem banco

#### W4.8: Criar Testes para Cache - Busca (Search)
- [ ] Teste: Primeira busca consulta banco de dados (cache miss)
- [ ] Teste: Segunda busca idêntica retorna do cache (cache hit)
- [ ] Teste: Busca com filtros diferentes não usa cache anterior
- [ ] Teste: Criar cliente invalida cache de busca
- [ ] Teste: Atualizar cliente invalida cache de busca
- [ ] Teste: Remover cliente invalida cache de busca
- [ ] Teste: Cache expira após TTL configurado

#### W4.9: Criar Testes para Cache - GetById
- [ ] Teste: Primeira busca por Id consulta banco de dados (cache miss)
- [ ] Teste: Segunda busca pelo mesmo Id retorna do cache (cache hit)
- [ ] Teste: Buscar outro Id não usa cache do Id anterior
- [ ] Teste: Atualizar cliente invalida cache específico do cliente
- [ ] Teste: Atualização parcial (PATCH) invalida cache específico do cliente
- [ ] Teste: Remover cliente invalida cache específico do cliente
- [ ] Teste: Cache expira após TTL configurado

#### W4.10: Criar Testes para Invalidação de Cache
- [ ] Teste: Criar cliente invalida cache de listagem e busca
- [ ] Teste: Atualizar cliente invalida cache do cliente, listagem e busca
- [ ] Teste: PATCH invalida cache do cliente, listagem e busca
- [ ] Teste: Remover cliente invalida cache do cliente, listagem e busca
- [ ] Teste: Invalidar cache não afeta sucesso da operação (mesmo se Redis falhar)

#### W4.11: Criar Testes para Observabilidade - Traces
- [ ] Configurar `InMemoryExporter<Activity>` para capturar traces
- [ ] Teste: Requisição HTTP cria trace principal
- [ ] Teste: Command handlers criam spans filhos
- [ ] Teste: Query handlers criam spans filhos
- [ ] Teste: Spans contêm tags relevantes
- [ ] Teste: Exceções são registradas nos spans
- [ ] Teste: Correlation ID está presente nos traces

#### W4.12: Criar Testes para Observabilidade - Métricas
- [ ] Configurar `InMemoryExporter<Metric>` para capturar métricas
- [ ] Teste: Criar cliente incrementa métrica `clientes.criados`
- [ ] Teste: Atualizar cliente incrementa métrica `clientes.atualizados`
- [ ] Teste: Remover cliente incrementa métrica `clientes.removidos`
- [ ] Teste: Buscar clientes incrementa métrica `clientes.buscas`
- [ ] Teste: Cache hit incrementa métrica `cache.hits`
- [ ] Teste: Cache miss incrementa métrica `cache.misses`
- [ ] Teste: Invalidar cache incrementa métrica `cache.invalidations`
- [ ] Teste: Métricas de tempo de processamento são registradas

#### W4.13: Criar Testes para ProblemDetails
- [ ] Teste: Erro de validação retorna ProblemDetails com status 400
- [ ] Teste: Cliente não encontrado retorna ProblemDetails com status 404
- [ ] Teste: CPF/Email duplicado retorna ProblemDetails com status 409
- [ ] Teste: Erro interno retorna ProblemDetails com status 500
- [ ] Teste: ProblemDetails contém campos obrigatórios (type, title, status, detail)
- [ ] Teste: ProblemDetails contém traceId para rastreabilidade
- [ ] Teste: Dados sensíveis não aparecem em ProblemDetails

#### W4.14: Criar Testes de Performance com Cache
- [ ] Teste: Medir tempo de resposta sem cache (baseline)
- [ ] Teste: Medir tempo de resposta com cache hit
- [ ] Teste: Validar que cache hit é significativamente mais rápido (ex: >50% mais rápido)
- [ ] Teste: Medir throughput com cache habilitado vs desabilitado

#### W4.15: Criar Testes de Resiliência
- [ ] Teste: Se Redis falhar, aplicação continua funcionando (sem cache)
- [ ] Teste: Erro no cache não impede criação de cliente
- [ ] Teste: Erro no cache não impede atualização de cliente
- [ ] Teste: Erro no cache não impede busca de cliente
- [ ] Teste: Erro na telemetria não impede operações

#### W4.16: Validar Todos os Testes
- [ ] Executar todos os testes de integração
- [ ] Validar que todos os testes passam
- [ ] Validar cobertura de testes:
  - Todos os endpoints estão testados
  - Todos os cenários de sucesso estão testados
  - Todos os cenários de erro estão testados
  - Cache está testado em todos os cenários relevantes
  - Observabilidade está testada
- [ ] Gerar relatório de cobertura de testes

---

## 🌊 Wave 5: Documentação e Finalização

### Objetivo
Documentar todas as implementações, criar guias de uso e garantir que o projeto está completo e pronto para produção.

### Microtarefas

#### W5.1: Atualizar README.md Principal
- [ ] Adicionar seção "Funcionalidades Implementadas":
  - API RESTful completa com CRUD de clientes
  - CQRS com Commands e Queries
  - Cache com HybridCache/Redis
  - Observabilidade com OpenTelemetry
  - Logs estruturados com correlation ID
  - Métricas de negócio e performance
  - Traces distribuídos
- [ ] Adicionar seção "Arquitetura":
  - Diagrama de arquitetura (opcional)
  - Descrição das camadas
  - Padrões utilizados
- [ ] Adicionar seção "Tecnologias":
  - .NET 9
  - PostgreSQL
  - Redis
  - OpenTelemetry
  - Jaeger
  - Prometheus
  - Grafana
  - Mvp24Hours Framework
- [ ] Adicionar seção "Endpoints da API" com lista completa

#### W5.2: Criar Guia de Configuração
- [ ] Criar arquivo `docs/CONFIGURATION.md`
- [ ] Documentar todas as configurações disponíveis:
  - ConnectionStrings (PostgreSQL, Redis)
  - Cache settings (TTL, habilitação)
  - OpenTelemetry settings (endpoints, sampling, exporters)
  - Logging settings (níveis, formato)
- [ ] Documentar variáveis de ambiente suportadas
- [ ] Documentar configurações por ambiente (Development, Production)

#### W5.3: Criar Guia de Cache
- [ ] Criar arquivo `docs/CACHE.md`
- [ ] Documentar estratégia de cache implementada:
  - Qual tecnologia foi escolhida (HybridCache, Redis)
  - Onde o cache é aplicado
  - TTL configurado para cada tipo de cache
  - Estratégia de invalidação
  - Padrão de chaves de cache
- [ ] Documentar como habilitar/desabilitar cache
- [ ] Documentar como limpar cache (endpoint de diagnóstico)
- [ ] Documentar como monitorar cache (métricas)

#### W5.4: Criar Guia de Observabilidade
- [ ] Criar arquivo `docs/OBSERVABILITY.md`
- [ ] Documentar componentes de observabilidade:
  - **Logs**: Formato, níveis, correlation ID, mascaramento
  - **Traces**: Como visualizar no Jaeger, principais spans
  - **Métricas**: Métricas disponíveis, como consultar no Prometheus
- [ ] Documentar ferramentas de visualização:
  - Jaeger UI: URL, como buscar traces
  - Prometheus: URL, queries úteis
  - Grafana: URL, dashboards disponíveis
- [ ] Documentar métricas customizadas:
  - `clientes.criados`
  - `clientes.atualizados`
  - `clientes.removidos`
  - `clientes.buscas`
  - `clientes.processamento.tempo`
  - `cache.hits`
  - `cache.misses`
  - `cache.invalidations`
- [ ] Documentar queries úteis do Prometheus:
  - Taxa de requisições por endpoint
  - Tempo de resposta (percentis)
  - Taxa de erros
  - Cache hit rate

#### W5.5: Criar Guia de Desenvolvimento
- [ ] Criar arquivo `docs/DEVELOPMENT.md`
- [ ] Documentar pré-requisitos:
  - .NET 9 SDK
  - Docker e Docker Compose
  - IDE recomendada
- [ ] Documentar como configurar ambiente de desenvolvimento:
  - Clonar repositório
  - Restaurar pacotes
  - Subir infraestrutura com Docker Compose
  - Aplicar migrations
  - Executar aplicação
- [ ] Documentar como executar testes:
  - Testes unitários
  - Testes de integração
  - Gerar relatório de cobertura
- [ ] Documentar estrutura de pastas do projeto
- [ ] Documentar convenções de código

#### W5.6: Criar Guia de Deploy
- [ ] Criar arquivo `docs/DEPLOYMENT.md`
- [ ] Documentar estratégia de deploy recomendada
- [ ] Documentar variáveis de ambiente necessárias
- [ ] Documentar como configurar PostgreSQL em produção
- [ ] Documentar como configurar Redis em produção
- [ ] Documentar como configurar OpenTelemetry em produção
- [ ] Documentar health checks disponíveis
- [ ] Documentar monitoramento recomendado

#### W5.7: Criar Exemplos de Uso da API
- [ ] Criar arquivo `docs/API_EXAMPLES.md`
- [ ] Adicionar exemplos de curl/httpie para cada endpoint:
  - `POST /api/clientes` - Criar cliente
  - `GET /api/clientes` - Listar clientes
  - `GET /api/clientes/search` - Buscar clientes
  - `GET /api/clientes/{id}` - Obter cliente
  - `PUT /api/clientes/{id}` - Atualizar cliente
  - `PATCH /api/clientes/{id}` - Atualizar parcialmente
  - `DELETE /api/clientes/{id}` - Remover cliente
- [ ] Adicionar exemplos de requisição e resposta em JSON
- [ ] Adicionar exemplos de erros comuns e como resolvê-los

#### W5.8: Criar Coleção do Postman/Insomnia
- [ ] Criar coleção com todos os endpoints da API
- [ ] Adicionar exemplos de requisições válidas
- [ ] Adicionar exemplos de requisições inválidas (para testar validações)
- [ ] Configurar variáveis de ambiente (base URL, tokens)
- [ ] Exportar coleção para arquivo JSON
- [ ] Adicionar arquivo na pasta `docs/postman/` ou `docs/insomnia/`

#### W5.9: Atualizar Documentação Swagger/OpenAPI
- [ ] Validar que todos os endpoints estão documentados
- [ ] Validar que todos os DTOs estão documentados
- [ ] Adicionar descrições detalhadas para cada endpoint
- [ ] Adicionar exemplos de requisição/resposta
- [ ] Adicionar descrições de erros possíveis
- [ ] Adicionar informações de autenticação (se aplicável)
- [ ] Exportar especificação OpenAPI para arquivo `docs/openapi.json`

#### W5.10: Criar CHANGELOG.md
- [ ] Criar arquivo `CHANGELOG.md`
- [ ] Documentar mudanças por versão:
  - Versão 1.0.0: Implementação inicial (TAR-001 a TAR-006)
  - Versão 2.0.0: Padronização RESTful, Cache e Observabilidade (TAR-007 a TAR-009)
- [ ] Documentar features adicionadas
- [ ] Documentar breaking changes (se houver)
- [ ] Documentar bugs corrigidos

#### W5.11: Revisar e Atualizar .gitignore
- [ ] Validar que arquivos desnecessários estão no .gitignore:
  - `bin/`, `obj/`
  - `data/` (PostgreSQL, Redis, Prometheus, Grafana)
  - `*.user`
  - `.vs/`, `.vscode/`, `.idea/`
  - Logs locais
- [ ] Remover arquivos ignorados do repositório se necessário

#### W5.12: Criar Licença do Projeto
- [ ] Escolher licença apropriada (MIT, Apache 2.0, etc.)
- [ ] Criar arquivo `LICENSE`
- [ ] Adicionar informações de licença no README.md

#### W5.13: Revisar Código e Refatorar
- [ ] Revisar todos os arquivos criados
- [ ] Remover código comentado desnecessário
- [ ] Remover código duplicado
- [ ] Aplicar princípios SOLID
- [ ] Validar nomenclatura de variáveis, métodos e classes
- [ ] Validar formatação e espaçamento
- [ ] Validar que não há warnings de compilação

#### W5.14: Validar Segurança
- [ ] Validar que dados sensíveis não são expostos:
  - CPF e Email mascarados em logs e traces
  - Senhas de configuração não estão hardcoded
  - Connection strings não estão hardcoded
- [ ] Validar que erros não expõem stack traces em produção
- [ ] Validar que ProblemDetails não expõe informações internas
- [ ] Validar que não há vulnerabilidades conhecidas nos pacotes NuGet

#### W5.15: Executar Testes Finais
- [ ] Executar todos os testes unitários
- [ ] Executar todos os testes de integração
- [ ] Validar que todos os testes passam (100% de sucesso)
- [ ] Gerar relatório de cobertura de código
- [ ] Validar cobertura mínima (sugestão: >80%)

#### W5.16: Testar Aplicação End-to-End
- [ ] Subir toda a infraestrutura com Docker Compose
- [ ] Aplicar migrations no PostgreSQL
- [ ] Iniciar aplicação
- [ ] Testar todos os endpoints manualmente:
  - Criar vários clientes
  - Listar clientes com paginação
  - Buscar clientes com filtros
  - Obter clientes por Id
  - Atualizar clientes (PUT e PATCH)
  - Remover clientes
- [ ] Validar que cache está funcionando (observar logs de cache hit/miss)
- [ ] Validar observabilidade:
  - Acessar Jaeger e visualizar traces
  - Acessar Prometheus e consultar métricas
  - Acessar Grafana e visualizar dashboards
- [ ] Validar health checks

#### W5.17: Preparar para Entrega
- [ ] Validar que todos os itens do backlog estão implementados:
  - ✅ TAR-007: Padronização de rotas RESTful
  - ✅ TAR-008: Implementação de cache
  - ✅ TAR-009: Implementação de telemetria
- [ ] Validar que todos os critérios de aceite estão atendidos
- [ ] Criar tag de versão no Git: `v2.0.0`
- [ ] Atualizar README.md com status do projeto

---

## 📊 Checklist de Conclusão

### Wave 1: Padronização RESTful (TAR-007) ✅ CONCLUÍDA
- [x] Todas as rotas RESTful implementadas
- [x] Queries e Commands criados
- [x] Validators implementados
- [x] Handlers implementados
- [x] Endpoints configurados no controller
- [x] ProblemDetails configurado
- [x] Swagger/OpenAPI atualizado
- [x] Testes de integração passando (32/32)

### Wave 2: Cache (TAR-008) ✅ CONCLUÍDA
- [x] HybridCache (.NET 9) configurado com Redis como L2 opcional
- [x] ICacheService criado e implementado via HybridCacheService
- [x] Configurações de cache em appsettings.json
- [x] Helper de chaves de cache criado (CacheKeyHelper)
- [x] Cache implementado em todos os Query Handlers (List, Search, GetById)
- [x] Invalidação implementada em todos os Command Handlers (Create, Update, Patch, Delete)
- [x] Redis no docker-compose.yml
- [x] Endpoint de diagnóstico de cache (CacheController)
- [x] Testes passando (32/32) com cache desabilitado para isolamento
- [x] README.md atualizado com instruções de Redis e cache

### Wave 3: Observabilidade (TAR-009)
- [ ] OpenTelemetry configurado
- [ ] Logging estruturado implementado
- [ ] Tracing implementado (HTTP, EF Core, custom)
- [ ] Métricas implementadas (HTTP, runtime, custom)
- [ ] Métricas de negócio criadas
- [ ] Métricas de cache criadas
- [ ] Correlation ID configurado
- [ ] Mascaramento de dados sensíveis
- [ ] Jaeger, Prometheus e Grafana no docker-compose.yml
- [ ] Configuração do Prometheus
- [ ] Dashboards do Grafana
- [ ] Testes de observabilidade passando

### Wave 4: Testes
- [ ] Testes para novos endpoints (GET, PUT, PATCH, DELETE)
- [ ] Testes de cache (hit, miss, invalidação)
- [ ] Testes de observabilidade (traces, métricas)
- [ ] Testes de ProblemDetails
- [ ] Testes de performance
- [ ] Testes de resiliência
- [ ] Todos os testes passando (100%)

### Wave 5: Documentação
- [ ] README.md atualizado
- [ ] docs/CONFIGURATION.md criado
- [ ] docs/CACHE.md criado
- [ ] docs/OBSERVABILITY.md criado
- [ ] docs/DEVELOPMENT.md criado
- [ ] docs/DEPLOYMENT.md criado
- [ ] docs/API_EXAMPLES.md criado
- [ ] Coleção Postman/Insomnia criada
- [ ] CHANGELOG.md criado
- [ ] Swagger/OpenAPI completo
- [ ] Código revisado e refatorado
- [ ] Segurança validada
- [ ] Testes finais passando
- [ ] Aplicação testada end-to-end

---

## 📚 Referências e Recursos Mvp24Hours

### Consultar ANTES de Implementar

#### Padronização RESTful (Wave 1)
- `mvp24h_infrastructure_guide` → topic: `webapi`
- `mvp24h_infrastructure_guide` → topic: `webapi-advanced`
- `mvp24h_modernization_guide` → category: `apis`, feature: `problem-details`
- `mvp24h_modernization_guide` → category: `apis`, feature: `minimal-apis`
- `mvp24h_reference_guide` → topic: `documentation`
- `mvp24h_reference_guide` → topic: `api-versioning`

#### Cache (Wave 2)
- `mvp24h_modernization_guide` → category: `caching`, feature: `hybrid-cache`
- `mvp24h_infrastructure_guide` → topic: `caching`
- `mvp24h_infrastructure_guide` → topic: `caching-advanced`
- `mvp24h_infrastructure_guide` → topic: `caching-redis`
- `mvp24h_database_advisor` → verificar integração com Repository/UnitOfWork

#### Observabilidade (Wave 3)
- `mvp24h_observability_setup` → component: `overview`
- `mvp24h_observability_setup` → component: `logging`
- `mvp24h_observability_setup` → component: `tracing`
- `mvp24h_observability_setup` → component: `metrics`
- `mvp24h_observability_setup` → component: `exporters`
- `mvp24h_cqrs_guide` → topic: `cqrs-tracing`
- `mvp24h_cqrs_guide` → topic: `cqrs-telemetry`

---

## 🎯 Considerações Finais

### Priorização
As tarefas estão organizadas em waves por ordem de prioridade e dependência:
1. **Wave 1**: Padronização RESTful - Base para as demais funcionalidades
2. **Wave 2**: Cache - Melhoria de performance
3. **Wave 3**: Observabilidade - Monitoramento e diagnóstico
4. **Wave 4**: Testes - Validação de qualidade
5. **Wave 5**: Documentação - Facilitar uso e manutenção

### Dependências
- **Wave 2** depende de **Wave 1** estar completa (rotas corretas para aplicar cache)
- **Wave 3** pode ser implementada em paralelo, mas é recomendado após **Wave 1** e **Wave 2**
- **Wave 4** deve ser executada após cada wave para validação incremental
- **Wave 5** deve ser executada ao final de todas as waves

### Importância das Tools do Mvp24Hours
**CRÍTICO**: Cada wave possui uma seção "Pré-requisito: Consultar Mvp24Hours" com as ferramentas específicas que DEVEM ser consultadas antes da implementação. Isso garante:
- Uso correto dos recursos do framework
- Evitar reinventar soluções já existentes
- Seguir padrões e melhores práticas do Mvp24Hours
- Aproveitar funcionalidades nativas do .NET 9 quando integradas

### Estimativa de Esforço
- **Wave 1**: ~8-12 horas (padronização RESTful)
- **Wave 2**: ~6-8 horas (implementação de cache)
- **Wave 3**: ~10-14 horas (observabilidade completa)
- **Wave 4**: ~6-10 horas (testes de integração)
- **Wave 5**: ~4-6 horas (documentação)
- **Total**: ~34-50 horas

### Validação de Qualidade
Cada microtarefa possui critérios claros de conclusão. Validar:
- ✅ Código compila sem warnings
- ✅ Todos os testes passam
- ✅ Funcionalidade testada manualmente
- ✅ Documentação atualizada
- ✅ Padrões do Mvp24Hours respeitados
