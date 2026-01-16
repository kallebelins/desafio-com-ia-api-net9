using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Command para remoção de um cliente
/// </summary>
public record DeleteClienteCommand : IMediatorCommand<bool>
{
    /// <summary>
    /// ID do cliente a ser removido
    /// </summary>
    public Guid Id { get; init; }
}
