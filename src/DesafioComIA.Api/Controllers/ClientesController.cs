using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Application.Logic.Pagination;
using DesafioComIA.Application.Commands.Cliente;
using DesafioComIA.Application.Queries.Cliente;
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de clientes
/// </summary>
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
        var command = new CreateClienteCommand
        {
            Nome = dto.Nome,
            Cpf = dto.Cpf,
            Email = dto.Email
        };

        var result = await _sender.SendAsync(command, cancellationToken);

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
    /// Obtém um cliente específico por ID
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do cliente</returns>
    /// <response code="200">Cliente retornado com sucesso</response>
    /// <response code="400">ID inválido</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClienteDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetClienteByIdQuery { Id = id };
        var result = await _sender.SendAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Atualiza completamente um cliente existente (PUT)
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <param name="dto">Dados completos do cliente para atualização</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Cliente atualizado</returns>
    /// <response code="200">Cliente atualizado com sucesso</response>
    /// <response code="400">Erro de validação</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="409">CPF ou Email já cadastrado em outro cliente</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClienteDto>> Update(
        Guid id,
        [FromBody] UpdateClienteDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClienteCommand
        {
            Id = id,
            Nome = dto.Nome,
            Cpf = dto.Cpf,
            Email = dto.Email
        };

        var result = await _sender.SendAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Atualiza parcialmente um cliente existente (PATCH)
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <param name="dto">Dados parciais do cliente para atualização</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Cliente atualizado</returns>
    /// <response code="200">Cliente atualizado com sucesso</response>
    /// <response code="400">Erro de validação ou nenhum campo informado</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="409">CPF ou Email já cadastrado em outro cliente</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClienteDto>> Patch(
        Guid id,
        [FromBody] PatchClienteDto dto,
        CancellationToken cancellationToken)
    {
        var command = new PatchClienteCommand
        {
            Id = id,
            Nome = dto.Nome,
            Cpf = dto.Cpf,
            Email = dto.Email
        };

        var result = await _sender.SendAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove um cliente existente
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Nenhum conteúdo em caso de sucesso</returns>
    /// <response code="204">Cliente removido com sucesso</response>
    /// <response code="400">ID inválido</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteClienteCommand { Id = id };
        await _sender.SendAsync(command, cancellationToken);
        return NoContent();
    }
}
