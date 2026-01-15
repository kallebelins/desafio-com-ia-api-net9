using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Collections;
using Mvp24Hours.Core.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Mvp24HoursValidationException = Mvp24Hours.Core.Exceptions.ValidationException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DesafioComIA.Application.Exceptions;

namespace DesafioComIA.Api.Middleware;

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
            // Log exceptions based on their type
            // Business exceptions and validation errors are expected and should not be logged as errors
            if (ex is BusinessException || 
                ex is ClienteJaExisteException || 
                ex is ClienteNaoEncontradoException ||
                ex is Mvp24HoursValidationException || 
                ex is FluentValidation.ValidationException ||
                ex is ArgumentException ||
                ex is UnauthorizedAccessException ||
                ex is KeyNotFoundException)
            {
                _logger.LogWarning(ex, "Business or validation exception occurred: {ExceptionType}", ex.GetType().Name);
            }
            else
            {
                // Only log unexpected exceptions as errors
                _logger.LogError(ex, "An unhandled exception occurred");
            }
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Title = "An error occurred while processing your request",
            Status = (int)statusCode,
            Instance = context.Request.Path,
            Detail = exception.Message
        };

        switch (exception)
        {
            case ClienteJaExisteException clienteJaExisteEx:
                statusCode = HttpStatusCode.Conflict;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Cliente já existe";
                problemDetails.Detail = clienteJaExisteEx.Message;
                break;

            case ClienteNaoEncontradoException clienteNaoEncontradoEx:
                statusCode = HttpStatusCode.NotFound;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Cliente não encontrado";
                problemDetails.Detail = clienteNaoEncontradoEx.Message;
                break;

            case BusinessException businessEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Business rule violation";
                problemDetails.Detail = businessEx.Message;
                break;

            case Mvp24HoursValidationException validationEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Validation error";
                problemDetails.Detail = "One or more validation errors occurred";
                // Extract validation errors from Mvp24Hours ValidationException
                var validationErrors = ExtractValidationErrors(validationEx);
                if (validationErrors.Count > 0)
                {
                    problemDetails.Extensions["errors"] = validationErrors;
                }
                else
                {
                    // If we can't extract structured errors, include the message details
                    problemDetails.Detail = validationEx.Message;
                    
                    // Log exception details for debugging
                    _logger.LogWarning("Could not extract validation errors from exception. " +
                        "Type: {ExceptionType}, Message: {Message}, InnerException: {InnerException}, " +
                        "Properties: {Properties}", 
                        validationEx.GetType().FullName, 
                        validationEx.Message,
                        validationEx.InnerException?.GetType().FullName ?? "null",
                        string.Join(", ", validationEx.GetType().GetProperties().Select(p => p.Name)));
                }
                break;

            case FluentValidation.ValidationException fluentValidationEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Validation error";
                problemDetails.Detail = "One or more validation errors occurred";
                problemDetails.Extensions["errors"] = fluentValidationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                break;

            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Invalid argument";
                problemDetails.Detail = argEx.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = "You are not authorized to perform this action";
                break;

            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Resource not found";
                problemDetails.Detail = exception.Message;
                break;

            default:
                // For production, don't expose internal exception details
                if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                {
                    problemDetails.Extensions["exception"] = exception.ToString();
                    problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                }
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        return context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Extracts validation errors from Mvp24Hours ValidationException.
    /// The exception may contain errors in different formats:
    /// - Errors property (IDictionary&lt;string, string[]&gt;)
    /// - Failures property (IEnumerable&lt;ValidationFailure&gt;)
    /// - InnerException as FluentValidation.ValidationException
    /// - Message string with format "field: message; field: message"
    /// </summary>
    private Dictionary<string, string[]> ExtractValidationErrors(Mvp24HoursValidationException exception)
    {
        var errors = new Dictionary<string, string[]>();

        _logger.LogDebug("Attempting to extract validation errors. Exception type: {Type}", exception.GetType().FullName);

        // Try to get ValidationErrors property (Mvp24Hours specific)
        var validationErrorsProperty = exception.GetType().GetProperty("ValidationErrors", 
            BindingFlags.Public | BindingFlags.Instance);
        
        if (validationErrorsProperty != null)
        {
            var validationErrorsValue = validationErrorsProperty.GetValue(exception);
            _logger.LogDebug("Found ValidationErrors property. Value type: {Type}, IsNull: {IsNull}", 
                validationErrorsValue?.GetType().FullName ?? "null", validationErrorsValue == null);
            
            if (validationErrorsValue is IEnumerable enumerable)
            {
                var extractedErrors = ExtractFromMessageResults(enumerable);
                if (extractedErrors.Count > 0)
                {
                    _logger.LogDebug("Extracted {Count} errors from ValidationErrors property", extractedErrors.Count);
                    return extractedErrors;
                }
            }
        }
        else
        {
            _logger.LogDebug("ValidationErrors property not found");
        }

        // Try to get Errors property using reflection
        var errorsProperty = exception.GetType().GetProperty("Errors", 
            BindingFlags.Public | BindingFlags.Instance);
        
        if (errorsProperty != null)
        {
            var errorsValue = errorsProperty.GetValue(exception);
            _logger.LogDebug("Found Errors property. Value type: {Type}, IsNull: {IsNull}", 
                errorsValue?.GetType().FullName ?? "null", errorsValue == null);
            
            // Handle IDictionary<string, string[]>
            if (errorsValue is IDictionary<string, string[]> dictErrors)
            {
                _logger.LogDebug("Extracted {Count} errors from Errors property as IDictionary<string, string[]>", dictErrors.Count);
                return new Dictionary<string, string[]>(dictErrors);
            }
            
            // Handle IEnumerable<ValidationFailure> from FluentValidation
            if (errorsValue is IEnumerable<ValidationFailure> failures)
            {
                var result = failures
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => f.ErrorMessage).ToArray()
                    );
                _logger.LogDebug("Extracted {Count} errors from Errors property as IEnumerable<ValidationFailure>", result.Count);
                return result;
            }
        }
        else
        {
            _logger.LogDebug("Errors property not found");
        }

        // Try to get Failures property using reflection
        var failuresProperty = exception.GetType().GetProperty("Failures", 
            BindingFlags.Public | BindingFlags.Instance);
        
        if (failuresProperty != null)
        {
            var failuresValue = failuresProperty.GetValue(exception);
            _logger.LogDebug("Found Failures property. Value type: {Type}, IsNull: {IsNull}", 
                failuresValue?.GetType().FullName ?? "null", failuresValue == null);
            
            if (failuresValue is IEnumerable<ValidationFailure> failures)
            {
                var result = failures
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => f.ErrorMessage).ToArray()
                    );
                _logger.LogDebug("Extracted {Count} errors from Failures property", result.Count);
                return result;
            }
        }
        else
        {
            _logger.LogDebug("Failures property not found");
        }

        // Check if InnerException is a FluentValidation.ValidationException
        // Search through the entire exception chain
        var currentException = exception.InnerException;
        while (currentException != null)
        {
            if (currentException is FluentValidation.ValidationException fluentEx)
            {
                var result = fluentEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogDebug("Extracted {Count} errors from InnerException chain (FluentValidation.ValidationException)", result.Count);
                return result;
            }
            currentException = currentException.InnerException;
        }
        
        if (exception.InnerException != null)
        {
            _logger.LogDebug("Searched through InnerException chain but found no FluentValidation.ValidationException. " +
                "First InnerException type: {Type}", exception.InnerException.GetType().FullName);
        }

        // Check exception Data dictionary for validation errors
        if (exception.Data.Contains("Errors"))
        {
            var dataErrors = exception.Data["Errors"];
            _logger.LogDebug("Found 'Errors' in Data dictionary. Value type: {Type}", 
                dataErrors?.GetType().FullName ?? "null");
            
            if (dataErrors is IDictionary<string, string[]> dictErrors)
            {
                _logger.LogDebug("Extracted {Count} errors from Data['Errors']", dictErrors.Count);
                return new Dictionary<string, string[]>(dictErrors);
            }
        }

        // Last resort: parse the message string
        // Expected format: "field: message; field: message"
        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            _logger.LogDebug("Attempting to parse validation message: {Message}", exception.Message);
            try
            {
                var parsedErrors = ParseValidationMessage(exception.Message);
                if (parsedErrors.Count > 0)
                {
                    _logger.LogDebug("Parsed {Count} validation errors from exception message", parsedErrors.Count);
                    return parsedErrors;
                }
                else
                {
                    _logger.LogDebug("ParseValidationMessage returned 0 errors");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse validation message: {Message}", exception.Message);
            }
        }

        _logger.LogWarning("Could not extract validation errors from exception using any method");
        return errors;
    }

    /// <summary>
    /// Extracts validation errors from IEnumerable of IMessageResult objects
    /// </summary>
    private Dictionary<string, string[]> ExtractFromMessageResults(IEnumerable messageResults)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var item in messageResults)
        {
            if (item == null) continue;

            try
            {
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
                        _logger.LogDebug("Extracted validation error - Key: {Key}, Message: {Message}", key, message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract error from IMessageResult item");
            }
        }

        return errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()
        );
    }

    /// <summary>
    /// Parses validation errors from a message string.
    /// Expected formats:
    /// - "Validation failed for CreateClienteCommand" (generic, returns empty)
    /// - "field: message; field: message"
    /// - "Cpf: O CPF informado é inválido.; Email: O e-mail informado é inválido."
    /// - Multi-line with errors listed
    /// </summary>
    private Dictionary<string, string[]> ParseValidationMessage(string message)
    {
        var errors = new Dictionary<string, List<string>>();

        // If message only contains the generic "Validation failed", look for details after it
        var validationFailedPrefix = "Validation failed for ";
        if (message.Contains(validationFailedPrefix))
        {
            // Try to extract error details that might come after the command name
            // Example: "Validation failed for CreateClienteCommand with 2 error(s): Cpf: ...; Email: ..."
            var detailsIndex = message.IndexOf(':');
            if (detailsIndex > 0 && detailsIndex < message.Length - 1)
            {
                // Get everything after the first colon following "Validation failed for"
                var afterCommand = message.Substring(detailsIndex + 1).Trim();
                
                // Check if it contains field-level errors
                if (afterCommand.Contains(":") && !afterCommand.StartsWith("Validation failed", StringComparison.OrdinalIgnoreCase))
                {
                    message = afterCommand;
                    _logger.LogDebug("Extracted details from validation message: {Details}", message);
                }
                else
                {
                    // No detailed errors found
                    _logger.LogDebug("No detailed errors found after 'Validation failed for'");
                    return new Dictionary<string, string[]>();
                }
            }
            else
            {
                // Just the generic message without details
                _logger.LogDebug("Generic validation message without details");
                return new Dictionary<string, string[]>();
            }
        }

        // Split by semicolon or newline
        var parts = message.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        _logger.LogDebug("Split message into {Count} parts", parts.Length);

        foreach (var part in parts)
        {
            var colonIndex = part.IndexOf(':');
            if (colonIndex > 0)
            {
                var field = part.Substring(0, colonIndex).Trim();
                var errorMessage = part.Substring(colonIndex + 1).Trim();

                // Clean up field name - remove any prefixes like "error(s)", numbers, etc.
                field = CleanFieldName(field);

                if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(errorMessage))
                {
                    if (!errors.ContainsKey(field))
                    {
                        errors[field] = new List<string>();
                    }
                    errors[field].Add(errorMessage);
                    _logger.LogDebug("Parsed error - Field: {Field}, Message: {Message}", field, errorMessage);
                }
            }
        }

        return errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()
        );
    }

    /// <summary>
    /// Cleans up field names extracted from validation messages
    /// </summary>
    private string CleanFieldName(string field)
    {
        // Remove common prefixes/patterns
        field = field.Trim();
        
        // Remove patterns like "2 error(s)", "with errors", etc.
        var cleanPatterns = new[] { "error(s)", "errors", "with", "and" };
        foreach (var pattern in cleanPatterns)
        {
            if (field.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                field = field.Substring(0, field.Length - pattern.Length).Trim();
            }
        }

        // If field starts with a number and space, remove it (e.g., "2 Cpf" -> "Cpf")
        var spaceIndex = field.IndexOf(' ');
        if (spaceIndex > 0 && int.TryParse(field.Substring(0, spaceIndex), out _))
        {
            field = field.Substring(spaceIndex + 1).Trim();
        }

        return field;
    }
}
