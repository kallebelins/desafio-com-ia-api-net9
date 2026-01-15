using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Domain.Entities;
using DesafioComIA.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DesafioComIA.Api.IntegrationTests;

public class ClientesControllerTests : IClassFixture<PostgreSqlContainerFixture>, IDisposable
{
    private readonly PostgreSqlContainerFixture _containerFixture;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceScope _scope;

    public ClientesControllerTests(PostgreSqlContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
        _factory = new CustomWebApplicationFactory<Program>(_containerFixture.ConnectionString);
        _client = _factory.CreateClient();
        
        // Criar scope para acessar o DbContext
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Garantir que o banco está criado e aplicar migrations
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Create_ComDadosValidos_DeveRetornar201Created()
    {
        // Arrange
        var createDto = new CreateClienteDto
        {
            Nome = "João Silva",
            Cpf = "11144477735", // CPF válido para testes
            Email = "joao.silva@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ComDadosValidos_DeveRetornarDadosCorretos()
    {
        // Arrange
        var createDto = new CreateClienteDto
        {
            Nome = "Maria Santos",
            Cpf = "12345678909", // CPF válido para testes
            Email = "maria.santos@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        var content = await response.Content.ReadAsStringAsync();
        var clienteDto = JsonSerializer.Deserialize<ClienteDto>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        clienteDto.Should().NotBeNull();
        clienteDto!.Nome.Should().Be(createDto.Nome);
        clienteDto.Cpf.Should().Be(createDto.Cpf);
        clienteDto.Email.Should().Be(createDto.Email);
        clienteDto.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_ComDadosValidos_DevePersistirNoBanco()
    {
        // Arrange
        var createDto = new CreateClienteDto
        {
            Nome = "Pedro Oliveira",
            Cpf = "98765432100", // CPF válido para testes
            Email = "pedro.oliveira@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        var content = await response.Content.ReadAsStringAsync();
        var clienteDto = JsonSerializer.Deserialize<ClienteDto>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verificar persistência no banco
        var clienteNoBanco = await _dbContext.Clientes
            .FirstOrDefaultAsync(c => c.Id == clienteDto!.Id);

        clienteNoBanco.Should().NotBeNull();
        clienteNoBanco!.Nome.Should().Be(createDto.Nome);
        clienteNoBanco.Cpf.Value.Should().Be(createDto.Cpf);
        clienteNoBanco.Email.Value.Should().Be(createDto.Email);
    }

    [Fact]
    public async Task Create_ComCpfDuplicado_DeveRetornar409Conflict()
    {
        // Arrange
        var primeiroCliente = new CreateClienteDto
        {
            Nome = "Cliente Original",
            Cpf = "11144477735", // CPF válido para testes
            Email = "original@example.com"
        };

        // Criar primeiro cliente
        await _client.PostAsJsonAsync("/api/clientes", primeiroCliente);

        var clienteDuplicado = new CreateClienteDto
        {
            Nome = "Cliente Duplicado",
            Cpf = "11144477735", // Mesmo CPF
            Email = "duplicado@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", clienteDuplicado);
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(409);
        problemDetails.Title.Should().Contain("Cliente já existe");
    }

    [Fact]
    public async Task Create_ComEmailDuplicado_DeveRetornar409Conflict()
    {
        // Arrange
        var primeiroCliente = new CreateClienteDto
        {
            Nome = "Cliente Original",
            Cpf = "12345678909", // CPF válido para testes
            Email = "original@example.com"
        };

        // Criar primeiro cliente
        await _client.PostAsJsonAsync("/api/clientes", primeiroCliente);

        var clienteDuplicado = new CreateClienteDto
        {
            Nome = "Cliente Duplicado",
            Cpf = "98765432100", // CPF válido diferente
            Email = "original@example.com" // Mesmo email
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", clienteDuplicado);
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(409);
        problemDetails.Title.Should().Contain("Cliente já existe");
    }

    [Fact]
    public async Task Create_ComCpfInvalido_DeveRetornar400BadRequest()
    {
        // Arrange
        var createDto = new CreateClienteDto
        {
            Nome = "Cliente Teste",
            Cpf = "123456789", // CPF inválido (muito curto)
            Email = "teste@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Title.Should().Contain("Validation error");
    }

    [Fact]
    public async Task Create_ComEmailInvalido_DeveRetornar400BadRequest()
    {
        // Arrange
        var createDto = new CreateClienteDto
        {
            Nome = "Cliente Teste",
            Cpf = "11144477735", // CPF válido
            Email = "email-invalido" // Email inválido (sem @ e domínio)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Title.Should().Contain("Validation error");
    }

    [Fact]
    public async Task Create_ComNomeMuitoCurto_DeveRetornar400BadRequest()
    {
        // Arrange
        var createDto = new CreateClienteDto
        {
            Nome = "AB", // Menos de 3 caracteres
            Cpf = "11144477735", // CPF válido
            Email = "teste@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Title.Should().Contain("Validation error");
    }

    [Fact]
    public async Task Create_ComNomeMuitoLongo_DeveRetornar400BadRequest()
    {
        // Arrange
        var nomeMuitoLongo = new string('A', 201); // Mais de 200 caracteres
        var createDto = new CreateClienteDto
        {
            Nome = nomeMuitoLongo,
            Cpf = "11144477735", // CPF válido
            Email = "teste@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Title.Should().Contain("Validation error");
    }

    public void Dispose()
    {
        // Limpar dados de teste após cada teste
        try
        {
            _dbContext?.Clientes.RemoveRange(_dbContext.Clientes);
            _dbContext?.SaveChanges();
        }
        catch
        {
            // Ignorar erros durante limpeza
        }
        
        _scope?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }
}
