# Desafio com IA - API de Cliente .NET 9

## 📖 Sobre o Projeto

Este é um **projeto colaborativo em comunidade** focado no treinamento e prática do uso de **Inteligência Artificial (IA) para desenvolvimento de software** utilizando o **MCP (Model Context Protocol) do Mvp24Hours** e seguindo a metodologia **MDPE Framework (Engenharia de Prompt Orientada a Microtarefas)**.

O objetivo principal é desenvolver uma API REST completa para gerenciamento de clientes, aplicando boas práticas de arquitetura de software e explorando as capacidades de desenvolvimento assistido por IA através do framework Mvp24Hours, utilizando a abordagem de microtarefas do MDPE Framework.

## 🎯 Objetivos

- **Treinar o uso de IA para desenvolvimento**: Explorar como a IA pode acelerar e melhorar o processo de desenvolvimento de software
- **Aprender Mvp24Hours Framework**: Dominar o uso do framework Mvp24Hours através do MCP
- **Praticar MDPE Framework**: Aplicar a metodologia de Engenharia de Prompt Orientada a Microtarefas para desenvolvimento estruturado e incremental
- **Praticar Arquitetura CQRS**: Implementar padrões de arquitetura modernos e escaláveis
- **Desenvolvimento Colaborativo**: Trabalhar em equipe seguindo metodologias ágeis

## 🔧 Metodologia de Desenvolvimento

### MDPE Framework (Engenharia de Prompt Orientada a Microtarefas)

Este projeto utiliza o **MDPE Framework** como metodologia de desenvolvimento, que consiste em:

- **Decomposição em Microtarefas**: Quebra de funcionalidades complexas em tarefas pequenas e gerenciáveis
- **Prompts Estruturados**: Uso de prompts bem definidos e específicos para cada microtarefa
- **Desenvolvimento Incremental**: Implementação em waves (ondas) progressivas, garantindo entregas incrementais
- **Validação Contínua**: Validação de cada microtarefa antes de avançar para a próxima
- **Documentação Contextual**: Cada microtarefa possui contexto suficiente para ser implementada de forma independente

As microtarefas estão organizadas em **Waves** no arquivo [Tasks Transformation](tasks/tasks-transformation.md), permitindo um desenvolvimento estruturado e acompanhável.

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas com separação de responsabilidades:

- **Arquitetura**: CQRS (Command Query Responsibility Segregation)
- **Framework**: Mvp24Hours .NET 9
- **Banco de Dados**: PostgreSQL
- **Padrões**: Repository, Unit of Work, Mediator

### Estrutura do Projeto

```
src/
├── DesafioComIA.Api/              # Camada de API (Controllers, Middleware)
├── DesafioComIA.Application/       # Camada de Aplicação (Commands, Queries, DTOs)
├── DesafioComIA.Domain/           # Camada de Domínio (Entities, Value Objects)
└── DesafioComIA.Infrastructure/   # Camada de Infraestrutura (Data Access, DbContext)
```

## 🚀 Tecnologias Utilizadas

- **.NET 9**: Framework principal
- **Mvp24Hours**: Framework de desenvolvimento com suporte a CQRS, Repository Pattern e muito mais
- **PostgreSQL**: Banco de dados relacional
- **Entity Framework Core**: ORM para acesso a dados
- **FluentValidation**: Validação de dados
- **AutoMapper**: Mapeamento de objetos
- **Swagger/OpenAPI**: Documentação da API

## 📋 Funcionalidades

### TAR-001: Cadastro de Cliente
- Cadastro de novos clientes com validações completas
- Validação de CPF e Email únicos
- Validação de formato de CPF e Email

### TAR-002: Listagem de Clientes
- Listagem paginada de clientes
- Ordenação customizável
- Suporte a grandes volumes de dados

### TAR-003: Filtro por Nome
- Busca parcial por nome
- Case-insensitive
- Suporte a espaços em branco

### TAR-004: Filtro por CPF
- Busca exata por CPF
- Aceita CPF com ou sem formatação
- Normalização automática

### TAR-005: Filtro por Email
- Busca exata por Email
- Case-insensitive
- Validação de formato

### TAR-006: Combinação de Filtros
- Aplicação de múltiplos filtros simultaneamente
- Operador AND entre filtros
- Validações individuais mantidas

## 📁 Documentação de Tarefas

Este projeto possui documentação detalhada das tarefas:

- **[Backlog de Tarefas](tasks/tasks-backlog.md)**: Lista completa de funcionalidades com regras de negócio e critérios de aceite
- **[Tasks Transformation](tasks/tasks-transformation.md)**: Microtarefas detalhadas organizadas em waves seguindo a metodologia MDPE Framework para implementação incremental

## 🛠️ Configuração e Execução

### Pré-requisitos

- .NET 9 SDK
- PostgreSQL (versão 12 ou superior)
- Visual Studio 2022, VS Code ou Rider

### Configuração do Banco de Dados

#### Opção 1: Usando Docker Compose (Recomendado)

1. Suba o container PostgreSQL usando Docker Compose:
```bash
docker-compose up -d
```

2. O banco de dados será criado automaticamente com as seguintes configurações:
   - **Database**: DesafioComIA
   - **User**: postgres
   - **Password**: postgres
   - **Port**: 5432

3. Para parar o container:
```bash
docker-compose down
```

#### Opção 2: PostgreSQL Local

1. Crie um banco de dados PostgreSQL:
```sql
CREATE DATABASE DesafioComIA;
```

2. Configure a connection string no `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Pooling=true;Database=DesafioComIA;User Id=postgres;Password=postgres;"
  }
}
```

### Executando o Projeto

1. Clone o repositório:
```bash
git clone <repository-url>
cd desafio-com-ia-api-net9
```

2. Restaure as dependências:
```bash
dotnet restore
```

3. Execute as migrations:
```bash
dotnet ef database update --project src/DesafioComIA.Infrastructure --startup-project src/DesafioComIA.Api
```

4. Execute a aplicação:
```bash
dotnet run --project src/DesafioComIA.Api
```

5. Acesse a documentação Swagger:
```
https://localhost:5001/swagger
```

## 🧪 Testes

Os testes de integração estão organizados por funcionalidade e cobrem:
- Cadastro de clientes válidos
- Validações de entrada
- Filtros individuais e combinados
- Paginação e ordenação

## 📚 Recursos de Aprendizado

### Mvp24Hours MCP

Este projeto utiliza o **MCP (Model Context Protocol) do Mvp24Hours** para desenvolvimento assistido por IA. O MCP fornece:

- Templates de arquitetura prontos
- Padrões de implementação validados
- Documentação contextual durante o desenvolvimento
- Guias de boas práticas

### Links Úteis

- [Mvp24Hours Documentation](https://github.com/mvp24hours)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [PostgreSQL .NET Documentation](https://www.npgsql.org/efcore/)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [AutoMapper Documentation](https://docs.automapper.org/)

## 🤝 Contribuindo

Este é um projeto colaborativo! Sinta-se à vontade para:

- Implementar novas funcionalidades seguindo as tarefas do backlog
- Melhorar a documentação
- Adicionar testes
- Compartilhar conhecimento sobre o uso de IA no desenvolvimento

## 📝 Licença

Este projeto é um projeto educacional e colaborativo.

## 👥 Comunidade

Este projeto faz parte de uma iniciativa comunitária para treinar e compartilhar conhecimento sobre desenvolvimento assistido por IA utilizando o framework Mvp24Hours.

---

**Desenvolvido com ❤️ pela comunidade usando IA, Mvp24Hours e MDPE Framework**
