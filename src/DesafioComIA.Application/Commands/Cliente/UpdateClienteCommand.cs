using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Command para atualização completa de um cliente (PUT)
/// </summary>
public record UpdateClienteCommand : IMediatorCommand<ClienteDto>
{
    /// <summary>
    /// ID do cliente a ser atualizado
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Nome do cliente
    /// </summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>
    /// CPF do cliente (com ou sem formatação)
    /// </summary>
    public string Cpf { get; init; } = string.Empty;

    /// <summary>
    /// E-mail do cliente
    /// </summary>
    public string Email { get; init; } = string.Empty;
}
