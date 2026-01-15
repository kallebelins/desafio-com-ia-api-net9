using System.Net;
using System.Text.Json;
using Mvp24Hours.Core.Exceptions;
using FluentValidation;
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
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
                problemDetails.Detail = validationEx.Message;
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
}
