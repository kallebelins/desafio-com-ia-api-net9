using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Application.Logic.Pagination;
using DesafioComIA.Application.Commands.Cliente;
using DesafioComIA.Application.Queries.Cliente;
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly ISender _sender;

    public ClientesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Cria um novo cliente
    /// </summary>
    /// <param name="dto">Dados do cliente a ser criado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Cliente criado</returns>
    /// <response code="201">Cliente criado com sucesso</response>
    /// <response code="400">Erro de validação</response>
    /// <response code="409">CPF ou Email já cadastrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClienteDto>> Create(
        [FromBody] CreateClienteDto dto,
        CancellationToken cancellationToken)
    {
        // Mapear DTO para Command
        var command = new CreateClienteCommand
        {
            Nome = dto.Nome,
            Cpf = dto.Cpf,
            Email = dto.Email
        };

        // Enviar comando via Mediator
        var result = await _sender.SendAsync(command, cancellationToken);

        // Retornar 201 Created com o resultado
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Lista todos os clientes com paginação e ordenação
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="sortBy">Campo de ordenação (Nome, Cpf, Email)</param>
    /// <param name="descending">Ordenação descendente</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de clientes</returns>
    /// <response code="200">Lista de clientes retornada com sucesso</response>
    /// <response code="400">Erro de validação</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClienteListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<ClienteListDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Nome",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new ListClientesQuery
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            Descending = descending
        };

        var result = await _sender.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Busca clientes com filtros opcionais
    /// </summary>
    /// <param name="nome">Filtro por nome (busca parcial, case-insensitive)</param>
    /// <param name="cpf">Filtro por CPF (busca exata, aceita com/sem formatação)</param>
    /// <param name="email">Filtro por email (busca exata, case-insensitive)</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="sortBy">Campo de ordenação (Nome, Cpf, Email)</param>
    /// <param name="descending">Ordenação descendente</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de clientes filtrados</returns>
    /// <response code="200">Lista de clientes retornada com sucesso</response>
    /// <response code="400">Erro de validação</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<ClienteListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<ClienteListDto>>> Search(
        [FromQuery] string? nome = null,
        [FromQuery] string? cpf = null,
        [FromQuery] string? email = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Nome",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetClientesQuery
        {
            Nome = nome,
            Cpf = cpf,
            Email = email,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            Descending = descending
        };

        var result = await _sender.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém um cliente por ID (placeholder para implementação futura)
    /// </summary>
    [HttpGet("{id}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetById(Guid id)
    {
        return NotFound();
    }
}
