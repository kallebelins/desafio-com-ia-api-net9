using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DesafioComIA.Infrastructure.Data;

namespace DesafioComIA.Api.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Adicionar configuração de teste com connection string do container
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _connectionString }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remover o DbContext original
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Adicionar DbContext com connection string do container
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });
        });

        builder.UseEnvironment("Testing");
    }
}
