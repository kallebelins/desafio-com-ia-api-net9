# Exemplos de Uso da API

Este documento contém exemplos práticos de requisições para todos os endpoints da API DesafioComIA.

## Sumário

- [Informações Gerais](#informações-gerais)
- [Criar Cliente (POST)](#criar-cliente-post)
- [Listar Clientes (GET)](#listar-clientes-get)
- [Buscar Clientes (GET /search)](#buscar-clientes-get-search)
- [Obter Cliente por ID (GET)](#obter-cliente-por-id-get)
- [Atualizar Cliente (PUT)](#atualizar-cliente-put)
- [Atualizar Parcialmente (PATCH)](#atualizar-parcialmente-patch)
- [Remover Cliente (DELETE)](#remover-cliente-delete)
- [Erros Comuns](#erros-comuns)

---

## Informações Gerais

### Base URL

```
http://localhost:5001/api
```

### Headers Padrão

```
Content-Type: application/json
Accept: application/json
```

### Formato de Resposta

Todas as respostas de sucesso retornam JSON. Erros retornam no formato **ProblemDetails** (RFC 7807).

---

## Criar Cliente (POST)

### Endpoint

```
POST /api/clientes
```

### Requisição

```bash
curl -X POST "http://localhost:5001/api/clientes" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João da Silva",
    "cpf": "123.456.789-00",
    "email": "joao.silva@email.com"
  }'
```

### Resposta de Sucesso (201 Created)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nome": "João da Silva",
  "cpf": "123.456.789-00",
  "email": "joao.silva@email.com"
}
```

**Headers da Resposta:**
```
Location: /api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### Exemplos de Dados Válidos

```json
// CPF com formatação
{
  "nome": "Maria Santos",
  "cpf": "987.654.321-00",
  "email": "maria@empresa.com.br"
}

// CPF sem formatação
{
  "nome": "Pedro Oliveira",
  "cpf": "11122233344",
  "email": "pedro.oliveira@gmail.com"
}
```

### Erros Possíveis

| Status | Causa |
|--------|-------|
| 400 | Nome, CPF ou Email inválidos |
| 409 | CPF ou Email já cadastrado |

---

## Listar Clientes (GET)

### Endpoint

```
GET /api/clientes
```

### Parâmetros de Query

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `page` | int | 1 | Número da página |
| `pageSize` | int | 10 | Itens por página (máx: 100) |

### Requisição

```bash
# Primeira página com 10 itens
curl "http://localhost:5001/api/clientes"

# Segunda página com 20 itens
curl "http://localhost:5001/api/clientes?page=2&pageSize=20"
```

### Resposta de Sucesso (200 OK)

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "nome": "João da Silva",
      "cpf": "123.456.789-00",
      "email": "joao.silva@email.com"
    },
    {
      "id": "7fb91a23-8823-4912-a1bc-3d852f77b1a8",
      "nome": "Maria Santos",
      "cpf": "987.654.321-00",
      "email": "maria@empresa.com.br"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 2,
  "totalPages": 1
}
```

---

## Buscar Clientes (GET /search)

### Endpoint

```
GET /api/clientes/search
```

### Parâmetros de Query

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `nome` | string | Filtro parcial por nome (case-insensitive) |
| `cpf` | string | Filtro exato por CPF (com ou sem formatação) |
| `email` | string | Filtro exato por email (case-insensitive) |
| `page` | int | Número da página |
| `pageSize` | int | Itens por página |

### Requisições

```bash
# Buscar por nome
curl "http://localhost:5001/api/clientes/search?nome=silva"

# Buscar por CPF
curl "http://localhost:5001/api/clientes/search?cpf=123.456.789-00"

# Buscar por email
curl "http://localhost:5001/api/clientes/search?email=joao@email.com"

# Combinar filtros
curl "http://localhost:5001/api/clientes/search?nome=joao&cpf=12345678900"

# Com paginação
curl "http://localhost:5001/api/clientes/search?nome=silva&page=1&pageSize=5"
```

### Resposta de Sucesso (200 OK)

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "nome": "João da Silva",
      "cpf": "123.456.789-00",
      "email": "joao.silva@email.com"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

### Exemplos de Busca por Nome

```bash
# Busca parcial - encontra "João da Silva", "Maria Silva", etc.
curl "http://localhost:5001/api/clientes/search?nome=Silva"

# Busca com espaços
curl "http://localhost:5001/api/clientes/search?nome=da%20Silva"
```

---

## Obter Cliente por ID (GET)

### Endpoint

```
GET /api/clientes/{id}
```

### Requisição

```bash
curl "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### Resposta de Sucesso (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nome": "João da Silva",
  "cpf": "123.456.789-00",
  "email": "joao.silva@email.com"
}
```

### Erros Possíveis

| Status | Causa |
|--------|-------|
| 400 | ID inválido (não é GUID válido) |
| 404 | Cliente não encontrado |

---

## Atualizar Cliente (PUT)

### Endpoint

```
PUT /api/clientes/{id}
```

### Requisição

```bash
curl -X PUT "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João da Silva Junior",
    "cpf": "123.456.789-00",
    "email": "joao.junior@novoemail.com"
  }'
```

### Resposta de Sucesso (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nome": "João da Silva Junior",
  "cpf": "123.456.789-00",
  "email": "joao.junior@novoemail.com"
}
```

### Comportamento

- **Todos os campos são obrigatórios**
- Substitui completamente os dados do cliente
- É idempotente (mesma requisição = mesmo resultado)

### Erros Possíveis

| Status | Causa |
|--------|-------|
| 400 | Dados inválidos (nome, CPF, email) |
| 404 | Cliente não encontrado |
| 409 | Novo CPF ou Email já existe em outro cliente |

---

## Atualizar Parcialmente (PATCH)

### Endpoint

```
PATCH /api/clientes/{id}
```

### Requisições

```bash
# Atualizar apenas o nome
curl -X PATCH "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva"
  }'

# Atualizar apenas o email
curl -X PATCH "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "novo.email@empresa.com"
  }'

# Atualizar nome e CPF
curl -X PATCH "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Carlos Silva",
    "cpf": "999.888.777-66"
  }'
```

### Resposta de Sucesso (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nome": "João Silva",
  "cpf": "123.456.789-00",
  "email": "joao.silva@email.com"
}
```

### Comportamento

- **Pelo menos um campo deve ser informado**
- Atualiza apenas os campos fornecidos
- Campos não informados permanecem inalterados
- É idempotente

### Erros Possíveis

| Status | Causa |
|--------|-------|
| 400 | Nenhum campo informado ou dados inválidos |
| 404 | Cliente não encontrado |
| 409 | Novo CPF ou Email já existe em outro cliente |

---

## Remover Cliente (DELETE)

### Endpoint

```
DELETE /api/clientes/{id}
```

### Requisição

```bash
curl -X DELETE "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### Resposta de Sucesso (204 No Content)

Sem corpo na resposta.

### Comportamento

- Remove permanentemente o cliente
- É idempotente (segunda chamada retorna 404)

### Erros Possíveis

| Status | Causa |
|--------|-------|
| 400 | ID inválido |
| 404 | Cliente não encontrado |

---

## Erros Comuns

### Formato ProblemDetails

Todos os erros retornam no formato ProblemDetails (RFC 7807):

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Erro de Validação",
  "status": 400,
  "detail": "Um ou mais erros de validação ocorreram.",
  "traceId": "00-abc123...-def456...-00",
  "errors": {
    "Nome": ["Nome é obrigatório"],
    "Cpf": ["CPF inválido"]
  }
}
```

### Erro 400 - Bad Request (Validação)

**Causa:** Dados inválidos na requisição.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Erro de Validação",
  "status": 400,
  "detail": "Um ou mais erros de validação ocorreram.",
  "errors": {
    "Nome": ["Nome deve ter no mínimo 3 caracteres"],
    "Cpf": ["CPF inválido"],
    "Email": ["Email deve ser um endereço de email válido"]
  }
}
```

**Solução:** Corrija os campos indicados no objeto `errors`.

### Erro 404 - Not Found

**Causa:** Cliente não encontrado.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Cliente não encontrado",
  "status": 404,
  "detail": "Cliente com Id '3fa85f64-...' não foi encontrado."
}
```

**Solução:** Verifique se o ID está correto.

### Erro 409 - Conflict

**Causa:** CPF ou Email já cadastrado em outro cliente.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflito",
  "status": 409,
  "detail": "Já existe um cliente cadastrado com o CPF informado."
}
```

**Soluções:**
- Use um CPF diferente
- Use um Email diferente
- Verifique se o cliente já existe

### Erro 500 - Internal Server Error

**Causa:** Erro interno do servidor.

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Erro Interno",
  "status": 500,
  "detail": "Ocorreu um erro interno. Por favor, tente novamente."
}
```

**Solução:** Verifique os logs da aplicação e o traceId para diagnóstico.

---

## Usando com HTTPie

Se você prefere [HTTPie](https://httpie.io/):

```bash
# Criar cliente
http POST localhost:5001/api/clientes nome="João Silva" cpf="123.456.789-00" email="joao@email.com"

# Listar clientes
http localhost:5001/api/clientes

# Buscar por nome
http localhost:5001/api/clientes/search nome==silva

# Obter por ID
http localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6

# Atualizar (PUT)
http PUT localhost:5001/api/clientes/3fa85f64-... nome="João Silva" cpf="123.456.789-00" email="novo@email.com"

# Atualizar parcial (PATCH)
http PATCH localhost:5001/api/clientes/3fa85f64-... nome="João Carlos"

# Remover
http DELETE localhost:5001/api/clientes/3fa85f64-...
```

---

## Usando com PowerShell

```powershell
# Criar cliente
Invoke-RestMethod -Uri "http://localhost:5001/api/clientes" -Method Post -ContentType "application/json" -Body '{"nome":"João Silva","cpf":"123.456.789-00","email":"joao@email.com"}'

# Listar clientes
Invoke-RestMethod -Uri "http://localhost:5001/api/clientes" -Method Get

# Buscar por nome
Invoke-RestMethod -Uri "http://localhost:5001/api/clientes/search?nome=silva" -Method Get

# Obter por ID
Invoke-RestMethod -Uri "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6" -Method Get

# Remover
Invoke-RestMethod -Uri "http://localhost:5001/api/clientes/3fa85f64-5717-4562-b3fc-2c963f66afa6" -Method Delete
```
