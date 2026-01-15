using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.Commands.Cliente;
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
    /// Obtém um cliente por ID (placeholder para implementação futura)
    /// </summary>
    [HttpGet("{id}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetById(Guid id)
    {
        return NotFound();
    }
}
