using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FluentValidation;
using DesafioComIA.Infrastructure.Data;
using DesafioComIA.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Desafio Com IA API",
        Version = "v1",
        Description = "API de Cliente com CQRS utilizando Mvp24Hours e PostgreSQL"
    });
});

// Configure PostgreSQL and DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Mvp24Hours DbContext
builder.Services.AddMvp24HoursDbContext<ApplicationDbContext>();

// Register Repository Async
builder.Services.AddMvp24HoursRepositoryAsync(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
});

// Register CQRS Mediator
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterLoggingBehavior = true;
    options.RegisterPerformanceBehavior = true;
    options.RegisterUnhandledExceptionBehavior = true;
    options.RegisterValidationBehavior = true;
    options.RegisterTransactionBehavior = true;
});

// Register FluentValidation
// Note: FluentValidation 12.x removed auto-validation methods
// Validation is handled by Mvp24Hours Mediator ValidationBehavior
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register AutoMapper using Mvp24Hours MapService
// Include Application assembly where DTOs and mappings will be defined
var applicationAssembly = System.Reflection.Assembly.Load("DesafioComIA.Application");
builder.Services.AddMvp24HoursMapService(
    typeof(Program).Assembly,
    applicationAssembly
);

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "postgresql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

// Configure ProblemDetails
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Desafio Com IA API v1");
        c.RoutePrefix = string.Empty; // Swagger UI na raiz
    });
}

// Use Exception Handling Middleware (must be early in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors();

// Map Health Check endpoint
app.MapHealthChecks("/health");

app.Run();
