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
using Mvp24Hours.Application.Logic.Pagination;

namespace DesafioComIA.Api.IntegrationTests;

public class ClientesQueryTests : IClassFixture<PostgreSqlContainerFixture>, IDisposable
{
    private readonly PostgreSqlContainerFixture _containerFixture;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceScope _scope;

    public ClientesQueryTests(PostgreSqlContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
        _factory = new CustomWebApplicationFactory<Program>(_containerFixture.ConnectionString);
        _client = _factory.CreateClient();
        
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    #region W4.14: Testes de Integração - Listagem Sem Filtros

    [Fact]
    public async Task List_SemFiltros_DeveRetornar200OK()
    {
        // Arrange - Criar alguns clientes
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("Maria Santos", "12345678909", "maria@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes?page=1&pageSize=10");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_Paginacao_DeveFuncionarCorretamente()
    {
        // Arrange - Criar 3 clientes  (simplificado para evitar problemas com CPFs)
        await CriarClienteTeste("Cliente 01", "11144477735", "cliente01@test.com");
        await CriarClienteTeste("Cliente 02", "12345678909", "cliente02@test.com");
        await CriarClienteTeste("Cliente 03", "98765432100", "cliente03@test.com");

        // Act - Pedir primeira página com 2 itens
        var response = await _client.GetAsync("/api/clientes?page=1&pageSize=2");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(3);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task List_OrdenacaoPorNome_DeveFuncionarAscendente()
    {
        // Arrange
        await CriarClienteTeste("Zacarias", "11144477735", "z@test.com");
        await CriarClienteTeste("Ana", "12345678909", "a@test.com");
        await CriarClienteTeste("Maria", "98765432100", "m@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes?sortBy=Nome&descending=false");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.First().Nome.Should().Be("Ana");
    }

    [Fact]
    public async Task List_ListaVazia_DeveRetornarArrayVazioComTotalCount0()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region W4.15: Testes de Integração - Filtro por Nome

    [Fact]
    public async Task Search_FiltroPorNomeParcial_DeveEncontrarClientes()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("Maria Silva", "12345678909", "maria@test.com");
        await CriarClienteTeste("Pedro Santos", "98765432100", "pedro@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?nome=Silva");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(c => c.Nome.Contains("Silva"));
    }

    [Fact]
    public async Task Search_FiltroPorNome_DeveSerCaseInsensitive()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?nome=silva");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_FiltroPorNomeComEspacos_DeveIgnorarEspacos()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?nome=%20Silva%20");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_TermoVazio_DeveRetornarTodosOsClientes()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("Maria Santos", "12345678909", "maria@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?nome=");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region W4.16: Testes de Integração - Filtro por CPF

    [Fact]
    public async Task Search_FiltroPorCpfExato_DeveEncontrarCliente()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("Maria Santos", "12345678909", "maria@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?cpf=11144477735");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Cpf.Should().Be("11144477735");
    }

    [Fact]
    public async Task Search_FiltroPorCpfComFormatacao_DeveAceitarEEncontrar()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?cpf=111.444.777-35");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_FiltroPorCpfSemFormatacao_DeveEncontrar()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?cpf=11144477735");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_FiltroPorCpfInexistente_DeveRetornarListaVazia()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?cpf=98765432100");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_FiltroPorCpfInvalido_DeveRetornar400BadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes/search?cpf=123");
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
    }

    #endregion

    #region W4.17: Testes de Integração - Filtro por Email

    [Fact]
    public async Task Search_FiltroPorEmailExato_DeveEncontrarCliente()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("Maria Santos", "12345678909", "maria@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?email=joao@test.com");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Email.Should().Be("joao@test.com");
    }

    [Fact]
    public async Task Search_FiltroPorEmail_DeveSerCaseInsensitive()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?email=JOAO@TEST.COM");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_FiltroPorEmailInexistente_DeveRetornarListaVazia()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?email=inexistente@test.com");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_FiltroPorEmailInvalido_DeveRetornar400BadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/clientes/search?email=emailinvalido");
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
    }

    [Fact]
    public async Task Search_FiltroPorEmailComEspacos_DeveIgnorarEspacos()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?email=%20joao@test.com%20");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    #endregion

    #region W4.18: Testes de Integração - Combinação de Filtros

    [Fact]
    public async Task Search_FiltroNomeECpf_DeveRetornarApenasClientesQueAtendemAmbos()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("João Santos", "12345678909", "joao2@test.com");
        await CriarClienteTeste("Maria Silva", "98765432100", "maria@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?nome=João&cpf=11144477735");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Nome.Should().Be("João Silva");
        result.Items.First().Cpf.Should().Be("11144477735");
    }

    [Fact]
    public async Task Search_FiltroNomeEEmail_DeveRetornarApenasClientesQueAtendemAmbos()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("João Santos", "12345678909", "joao2@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?nome=João&email=joao@test.com");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Nome.Should().Be("João Silva");
    }

    [Fact]
    public async Task Search_FiltroCpfEEmail_DeveRetornarApenasClientesQueAtendemAmbos()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?cpf=11144477735&email=joao@test.com");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_FiltroNomeCpfEEmail_DeveRetornarApenasClientesQueAtendemTodos()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");
        await CriarClienteTeste("João Santos", "12345678909", "joao2@test.com");

        // Act
        var response = await _client.GetAsync("/api/clientes/search?nome=João&cpf=11144477735&email=joao@test.com");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Nome.Should().Be("João Silva");
    }

    [Fact]
    public async Task Search_NenhumClienteAtendeTodosCriterios_DeveRetornarListaVazia()
    {
        // Arrange
        await CriarClienteTeste("João Silva", "11144477735", "joao@test.com");

        // Act - Buscar com email diferente do cadastrado
        var response = await _client.GetAsync("/api/clientes/search?nome=João&cpf=11144477735&email=outro@test.com");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ClienteListDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private async Task<ClienteDto> CriarClienteTeste(string nome, string cpf, string email)
    {
        var createDto = new CreateClienteDto
        {
            Nome = nome,
            Cpf = cpf,
            Email = email
        };

        var response = await _client.PostAsJsonAsync("/api/clientes", createDto);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ClienteDto>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    #endregion

    public void Dispose()
    {
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
