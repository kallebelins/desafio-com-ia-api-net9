using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.OpenApi.Models;
using FluentValidation;
using StackExchange.Redis;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using DesafioComIA.Infrastructure.Data;
using DesafioComIA.Infrastructure.Caching;
using DesafioComIA.Infrastructure.Configuration;
using DesafioComIA.Infrastructure.Services.Cache;
using DesafioComIA.Infrastructure.Telemetry;
using DesafioComIA.Api.Configuration;
using DesafioComIA.Api.Middleware;
using DesafioComIA.Application.Telemetry;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry Settings
var otelSettings = builder.Configuration.GetSection(OpenTelemetrySettings.SectionName).Get<OpenTelemetrySettings>() 
    ?? new OpenTelemetrySettings();
builder.Services.Configure<OpenTelemetrySettings>(builder.Configuration.GetSection(OpenTelemetrySettings.SectionName));

// Configure OpenTelemetry
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(
        serviceName: otelSettings.ServiceName,
        serviceVersion: otelSettings.ServiceVersion,
        serviceInstanceId: Environment.MachineName)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = builder.Environment.EnvironmentName
    });

// Configure Tracing
if (otelSettings.Tracing.Enabled)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(otelSettings.ServiceName, serviceVersion: otelSettings.ServiceVersion))
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext => 
                        !httpContext.Request.Path.StartsWithSegments("/health") &&
                        !httpContext.Request.Path.StartsWithSegments("/metrics");
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request.method", request.Method);
                        activity.SetTag("url.path", request.Path);
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
                })
                .AddSource(DiagnosticsConfig.ServiceName)
                .AddSource($"{DiagnosticsConfig.ServiceName}.Cache")
                .AddSource($"{DiagnosticsConfig.ServiceName}.Domain");

            // Add OTLP exporter
            if (!string.IsNullOrEmpty(otelSettings.Otlp.Endpoint))
            {
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otelSettings.Otlp.Endpoint);
                    options.Protocol = otelSettings.Otlp.Protocol.Equals("Grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;
                });
            }

            // Add Console exporter (development only)
            if (otelSettings.EnableConsoleExporter && builder.Environment.IsDevelopment())
            {
                tracing.AddConsoleExporter();
            }
        })
        .WithMetrics(metrics =>
        {
            metrics
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(ClienteMetrics.MeterName)
                .AddMeter(CacheMetrics.MeterName);

            // Add Prometheus exporter
            if (otelSettings.Metrics.Enabled)
            {
                metrics.AddPrometheusExporter();
            }

            // Add OTLP exporter for metrics
            if (!string.IsNullOrEmpty(otelSettings.Otlp.Endpoint))
            {
                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otelSettings.Otlp.Endpoint);
                    options.Protocol = otelSettings.Otlp.Protocol.Equals("Grpc", StringComparison.OrdinalIgnoreCase)
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;
                });
            }

            // Add Console exporter (development only)
            if (otelSettings.EnableConsoleExporter && builder.Environment.IsDevelopment())
            {
                metrics.AddConsoleExporter();
            }
        });
}

// Configure Logging with OpenTelemetry
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (otelSettings.Logging.Enabled)
{
    builder.Logging.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.IncludeFormattedMessage = otelSettings.Logging.IncludeFormattedMessage;
        options.IncludeScopes = otelSettings.Logging.IncludeScopes;
        options.ParseStateValues = true;

        // Add OTLP exporter for logs
        if (!string.IsNullOrEmpty(otelSettings.Otlp.Endpoint))
        {
            options.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otelSettings.Otlp.Endpoint);
                otlp.Protocol = otelSettings.Otlp.Protocol.Equals("Grpc", StringComparison.OrdinalIgnoreCase)
                    ? OtlpExportProtocol.Grpc
                    : OtlpExportProtocol.HttpProtobuf;
            });
        }

        // Add Console exporter (development only)
        if (otelSettings.EnableConsoleExporter && builder.Environment.IsDevelopment())
        {
            options.AddConsoleExporter();
        }
    });
}

// Register metrics services
builder.Services.AddSingleton<ClienteMetrics>();
builder.Services.AddSingleton<CacheMetrics>();

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

// Configure Controllers
builder.Services.AddControllers();

// Configure Native OpenAPI (.NET 9) - skip in Testing environment
if (!builder.Environment.IsEnvironment("Testing"))
{
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
}

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

// Load Application assembly for handlers, validators and mappings
var applicationAssembly = System.Reflection.Assembly.Load("DesafioComIA.Application");

// Register CQRS Mediator
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterHandlersFromAssembly(applicationAssembly);
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
builder.Services.AddValidatorsFromAssembly(applicationAssembly);

// Register AutoMapper using Mvp24Hours MapService
// Include Application assembly where DTOs and mappings will be defined
builder.Services.AddMvp24HoursMapService(applicationAssembly);

// Configure Cache Settings
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));

// Configure Redis Connection (optional, for L2 distributed cache)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Configure Redis as distributed cache backend for HybridCache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = builder.Configuration.GetSection("Cache:KeyPrefix").Value ?? "desafiocomia:";
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Warning] Não foi possível conectar ao Redis: {ex.Message}. Cache funcionará apenas em memória.");
    }
}

// Configure HybridCache (.NET 9)
var cacheSettings = builder.Configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>() ?? new CacheSettings();
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = cacheSettings.MaximumPayloadBytes;
    options.MaximumKeyLength = cacheSettings.MaximumKeyLength;
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(cacheSettings.DefaultTTLMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(cacheSettings.LocalCacheTTLMinutes)
    };
});

// Register Cache Service
builder.Services.AddSingleton<ICacheService, HybridCacheService>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "postgresql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

// Add Redis health check if configured
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddHealthChecks()
        .AddRedis(
            redisConnectionString,
            name: "redis",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
}

// Configure ProblemDetails
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    // Map Native OpenAPI document endpoint
    app.MapOpenApi("/openapi/{documentName}.json");
    
    // Enable Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "DesafioComIA API v1.0.0");
        options.RoutePrefix = "swagger";
        options.EnableDeepLinking();
        options.EnableFilter();
        options.EnableTryItOutByDefault();
        options.DisplayRequestDuration();
    });
}

// Use Correlation ID Middleware (must be first for proper tracing)
app.UseCorrelationId();

// Use Exception Handling Middleware (must be early in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors();

// Map Controllers
app.MapControllers();

// Map Health Check endpoint
app.MapHealthChecks("/health");

// Map Prometheus metrics endpoint
if (otelSettings.Metrics.Enabled)
{
    app.MapPrometheusScrapingEndpoint(otelSettings.Metrics.PrometheusEndpoint);
}

app.Run();

// Required for integration tests with WebApplicationFactory
public partial class Program { }
