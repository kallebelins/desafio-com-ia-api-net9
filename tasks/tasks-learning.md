# Aprendizados do Projeto

Este documento registra os aprendizados técnicos obtidos durante o desenvolvimento do projeto.

---

## 1. OpenAPI no .NET 9 - Native OpenAPI vs Swashbuckle

### Contexto

O .NET 9 introduziu suporte nativo a OpenAPI através do pacote `Microsoft.AspNetCore.OpenApi`, substituindo a necessidade de usar apenas Swashbuckle para geração de documentação.

### Problema Encontrado

O método `AddMvp24HoursNativeOpenApi` do pacote `Mvp24Hours.WebAPI` apresentou um bug onde o `MapMvp24HoursNativeOpenApi` **não registrava o middleware `UseSwaggerUI`**, causando erro 404 ao acessar a interface do Swagger UI.

### Solução Aplicada

Substituir o helper do Mvp24Hours pela implementação direta usando os métodos nativos:

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

### Pacotes Necessários

| Pacote | Origem | Função |
|--------|--------|--------|
| `Microsoft.AspNetCore.OpenApi` | Nativo .NET 9 | Geração do documento OpenAPI (JSON) |
| `Swashbuckle.AspNetCore` | Transitivo via Mvp24Hours.WebAPI | Interface visual (Swagger UI) |

### Pontos-Chave

1. **`AddOpenApi`** - Registra os serviços de geração do documento OpenAPI
2. **`AddDocumentTransformer`** - Permite customizar o documento OpenAPI (título, versão, descrição)
3. **`MapOpenApi`** - Expõe o endpoint JSON do documento OpenAPI (ex: `/openapi/v1.json`)
4. **`UseSwaggerUI`** - Configura a interface visual do Swagger UI
5. **Separação de responsabilidades** - O .NET 9 gera o documento OpenAPI, o Swashbuckle fornece apenas a UI

### Referências

- [ASP.NET Core OpenAPI documentation](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview)
- [.NET 9 OpenAPI support](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9/runtime#openapi)

---

## 2. Tratamento de Exceções de Validação do Mvp24Hours

### Contexto

O framework Mvp24Hours possui behaviors CQRS que interceptam comandos e fazem validação usando FluentValidation. Quando uma validação falha, o `ValidationBehavior` lança uma exceção `Mvp24Hours.Core.Exceptions.ValidationException` que precisa ser tratada adequadamente pela API.

### Problema Encontrado

Os erros de validação estavam sendo retornados na resposta sem os detalhes específicos dos campos:

**Resposta incorreta:**
```json
{
  "title": "Validation error",
  "status": 400,
  "detail": "Validation failed for CreateClienteCommand",
  "instance": "/api/Clientes"
}
```

**Log mostrando os detalhes (não expostos na resposta):**
```
[Validation] CreateClienteCommand failed validation with 2 error(s): 
Cpf: O CPF informado é inválido.; Email: O e-mail informado é inválido.
```

### Causa Raiz

A exceção `Mvp24Hours.Core.Exceptions.ValidationException` armazena os erros em uma propriedade **`ValidationErrors`** do tipo `IList<IMessageResult>`, não em propriedades padrão como `Errors` ou `Failures`.

Cada item `IMessageResult` possui:
- **`Key`**: Nome do campo (ex: "Cpf", "Email")
- **`Message`**: Mensagem de erro (ex: "O CPF informado é inválido.")

### Solução Implementada

#### 1. Extração específica da propriedade ValidationErrors

```csharp
// Try to get ValidationErrors property (Mvp24Hours specific)
var validationErrorsProperty = exception.GetType().GetProperty("ValidationErrors", 
    BindingFlags.Public | BindingFlags.Instance);

if (validationErrorsProperty != null)
{
    var validationErrorsValue = validationErrorsProperty.GetValue(exception);
    
    if (validationErrorsValue is IEnumerable enumerable)
    {
        var extractedErrors = ExtractFromMessageResults(enumerable);
        if (extractedErrors.Count > 0)
        {
            return extractedErrors;
        }
    }
}
```

#### 2. Método auxiliar para extrair IMessageResult

```csharp
private Dictionary<string, string[]> ExtractFromMessageResults(IEnumerable messageResults)
{
    var errors = new Dictionary<string, List<string>>();

    foreach (var item in messageResults)
    {
        if (item == null) continue;

        var itemType = item.GetType();
        
        // Try to get Key property (field name)
        var keyProperty = itemType.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
        var messageProperty = itemType.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
        
        if (keyProperty != null && messageProperty != null)
        {
            var key = keyProperty.GetValue(item)?.ToString();
            var message = messageProperty.GetValue(item)?.ToString();
            
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(message))
            {
                if (!errors.ContainsKey(key))
                {
                    errors[key] = new List<string>();
                }
                errors[key].Add(message);
            }
        }
    }

    return errors.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.ToArray()
    );
}
```

#### 3. Logging diferenciado por tipo de exceção

Exceções de negócio e validação são esperadas e não devem ser logadas como erros:

```csharp
if (ex is BusinessException || 
    ex is ClienteJaExisteException || 
    ex is ClienteNaoEncontradoException ||
    ex is Mvp24HoursValidationException || 
    ex is FluentValidation.ValidationException ||
    ex is ArgumentException ||
    ex is UnauthorizedAccessException ||
    ex is KeyNotFoundException)
{
    _logger.LogWarning(ex, "Business or validation exception occurred: {ExceptionType}", 
        ex.GetType().Name);
}
else
{
    _logger.LogError(ex, "An unhandled exception occurred");
}
```

### Resultado Final

**Resposta correta com detalhes dos erros:**
```json
{
  "title": "Validation error",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "instance": "/api/Clientes",
  "errors": {
    "Cpf": ["O CPF informado é inválido."],
    "Email": ["O e-mail informado é inválido."]
  }
}
```

### Pontos-Chave

1. **Reflection é necessária** - O Mvp24Hours não expõe interface pública para acessar `ValidationErrors`
2. **IMessageResult não é tipo concreto** - Usar reflection para acessar `Key` e `Message`
3. **Busca em cadeia** - Tentar múltiplas estratégias: ValidationErrors → Errors → Failures → InnerException
4. **Logging estruturado** - Warnings para exceções esperadas, Errors para inesperadas
5. **ProblemDetails padrão** - Usar `extensions["errors"]` para detalhes de validação

### Ordem de Tentativas de Extração

1. **ValidationErrors** (Mvp24Hours) - `IList<IMessageResult>`
2. **Errors** - `IDictionary<string, string[]>` ou `IEnumerable<ValidationFailure>`
3. **Failures** - `IEnumerable<ValidationFailure>`
4. **InnerException** - Busca recursiva por `FluentValidation.ValidationException`
5. **Data["Errors"]** - Dicionário de dados da exceção
6. **Message parsing** - Último recurso, parse da string da mensagem

### Tratamento Específico de Exceções de Negócio

| Exceção | Status Code | Título |
|---------|-------------|--------|
| `ClienteJaExisteException` | 409 Conflict | "Cliente já existe" |
| `ClienteNaoEncontradoException` | 404 Not Found | "Cliente não encontrado" |
| `ValidationException` | 400 Bad Request | "Validation error" |
| `BusinessException` | 400 Bad Request | "Business rule violation" |

### Referências

- [ASP.NET Core ProblemDetails](https://learn.microsoft.com/aspnet/core/web-api/handle-errors)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Mvp24Hours CQRS Behaviors](https://github.com/kallebelins/mvp24hours-dotnet)

---
