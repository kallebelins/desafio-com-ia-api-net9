using Testcontainers.PostgreSql;

namespace DesafioComIA.Api.IntegrationTests;

/// <summary>
/// Fixture compartilhada para gerenciar o container PostgreSQL durante os testes de integração.
/// O container é iniciado uma vez e reutilizado por todos os testes na mesma execução.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;
    private string? _connectionString;

    /// <summary>
    /// Connection string do PostgreSQL container para uso nos testes.
    /// </summary>
    public string ConnectionString
    {
        get
        {
            if (_connectionString == null)
            {
                throw new InvalidOperationException(
                    "Container não foi inicializado. Certifique-se de que InitializeAsync foi chamado.");
            }
            return _connectionString;
        }
    }

    /// <summary>
    /// Inicializa o container PostgreSQL antes dos testes serem executados.
    /// </summary>
    public async Task InitializeAsync()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("DesafioComIA_Test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgreSqlContainer.StartAsync();

        _connectionString = _postgreSqlContainer.GetConnectionString();
    }

    /// <summary>
    /// Limpa e remove o container PostgreSQL após todos os testes serem executados.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.DisposeAsync();
        }
    }
}
