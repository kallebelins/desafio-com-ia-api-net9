# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [2.0.0] - 2026-01-16

### Adicionado

#### TAR-007: Padronização de Rotas RESTful
- Novo endpoint `GET /api/clientes/{id}` para obter cliente por ID
- Novo endpoint `PUT /api/clientes/{id}` para atualização completa de cliente
- Novo endpoint `PATCH /api/clientes/{id}` para atualização parcial de cliente
- Novo endpoint `DELETE /api/clientes/{id}` para remoção de cliente
- Header `Location` no response do `POST /api/clientes` com URL do recurso criado
- Formato **ProblemDetails** (RFC 7807) para todas as respostas de erro
- Documentação Swagger/OpenAPI completa com exemplos e descrições
- XML comments em todos os endpoints para documentação automática
- Atributos `[ProducesResponseType]` para todos os status codes possíveis

#### TAR-008: Implementação de Cache
- **HybridCache** do .NET 9 com suporte a dois níveis (L1 memória + L2 Redis)
- Cache implementado em todas as operações de leitura:
  - Listagem de clientes (`ListClientesQueryHandler`)
  - Busca de clientes (`GetClientesQueryHandler`)
  - Obter cliente por ID (`GetClienteByIdQueryHandler`)
- Invalidação automática de cache em operações de escrita
- Classe `ICacheService` com abstração para operações de cache
- Helper `CacheKeyHelper` para geração consistente de chaves
- Configuração de TTL por tipo de operação via `appsettings.json`
- Serviço Redis adicionado ao `docker-compose.yml`
- Endpoint de diagnóstico de cache (`/api/cache/*`) para ambiente Development
- Resiliência: erros de cache não bloqueiam operações principais

#### TAR-009: Observabilidade com OpenTelemetry
- **OpenTelemetry** configurado com Tracing, Metrics e Logging
- **Jaeger** para visualização de traces distribuídos
- **Prometheus** para coleta e armazenamento de métricas
- **Grafana** com dashboards pré-configurados
- Métricas de negócio customizadas:
  - `clientes.criados` - Total de clientes criados
  - `clientes.atualizados` - Total de clientes atualizados
  - `clientes.removidos` - Total de clientes removidos
  - `clientes.buscas` - Total de buscas realizadas
  - `clientes.processamento.tempo` - Tempo de processamento das operações
- Métricas de cache:
  - `cache.hits` - Total de cache hits
  - `cache.misses` - Total de cache misses
  - `cache.invalidations` - Total de invalidações
- **Correlation ID** propagado em todas as requisições e logs
- **Mascaramento de dados sensíveis** (CPF e Email) em traces e logs
- Endpoint `/metrics` para scraping do Prometheus
- Instrumentação automática de HTTP, EF Core e operações customizadas
- Configuração de exporters via `appsettings.json`

### Alterado

- Entidade `Cliente` agora possui métodos `AtualizarNome()`, `AtualizarCpf()`, `AtualizarEmail()`
- Todos os Command Handlers agora incluem invalidação de cache
- Todos os Query Handlers agora utilizam cache para leitura
- `docker-compose.yml` expandido com serviços de observabilidade
- README.md atualizado com documentação completa das novas funcionalidades

### Documentação

- `docs/CONFIGURATION.md` - Guia completo de configuração
- `docs/CACHE.md` - Documentação da estratégia de cache
- `docs/OBSERVABILITY.md` - Guia de observabilidade
- `docs/DEVELOPMENT.md` - Guia de desenvolvimento
- `docs/DEPLOYMENT.md` - Guia de deploy
- `docs/API_EXAMPLES.md` - Exemplos de uso da API
- `CHANGELOG.md` - Este arquivo

---

## [1.0.0] - 2026-01-10

### Adicionado

#### TAR-001: Cadastro de Cliente
- Endpoint `POST /api/clientes` para criação de clientes
- Validação de CPF único no sistema
- Validação de Email único no sistema
- Validação de formato de CPF (com e sem formatação)
- Validação de formato de Email
- Value Objects `Cpf` e `Email` do Mvp24Hours
- `CreateClienteCommand` e `CreateClienteCommandHandler` (CQRS)
- `CreateClienteCommandValidator` com FluentValidation

#### TAR-002: Listagem de Clientes
- Endpoint `GET /api/clientes` para listagem paginada
- Suporte a paginação com parâmetros `page` e `pageSize`
- Ordenação padrão por nome
- `ListClientesQuery` e `ListClientesQueryHandler` (CQRS)

#### TAR-003: Filtro por Nome
- Busca parcial por nome (contém)
- Case-insensitive
- Suporte a espaços em branco

#### TAR-004: Filtro por CPF
- Busca exata por CPF
- Aceita CPF com ou sem formatação
- Normalização automática do CPF

#### TAR-005: Filtro por Email
- Busca exata por Email
- Case-insensitive

#### TAR-006: Combinação de Filtros
- Endpoint `GET /api/clientes/search` para busca com filtros
- Aplicação de múltiplos filtros simultaneamente (AND)
- `GetClientesQuery` e `GetClientesQueryHandler` (CQRS)
- `GetClientesQueryValidator` com FluentValidation

### Infraestrutura

- Arquitetura CQRS com Mvp24Hours
- PostgreSQL como banco de dados
- Entity Framework Core para ORM
- FluentValidation para validações
- AutoMapper para mapeamento de objetos
- Docker Compose para infraestrutura local
- Testes de integração com xUnit

### Estrutura do Projeto

```
src/
├── DesafioComIA.Api/           # Camada de API
├── DesafioComIA.Application/   # Camada de Aplicação (CQRS)
├── DesafioComIA.Domain/        # Camada de Domínio
└── DesafioComIA.Infrastructure/# Camada de Infraestrutura
```

---

## Tipos de Mudanças

- **Adicionado** para novas funcionalidades
- **Alterado** para mudanças em funcionalidades existentes
- **Descontinuado** para funcionalidades que serão removidas em breve
- **Removido** para funcionalidades removidas
- **Corrigido** para correções de bugs
- **Segurança** para correções de vulnerabilidades
