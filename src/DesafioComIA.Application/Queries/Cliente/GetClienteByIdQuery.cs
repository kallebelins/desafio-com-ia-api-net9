using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Query para obter um cliente específico por ID
/// </summary>
public record GetClienteByIdQuery : IMediatorQuery<ClienteDto>
{
    /// <summary>
    /// ID do cliente a ser buscado
    /// </summary>
    public Guid Id { get; init; }
}
