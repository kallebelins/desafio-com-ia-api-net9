using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Command para atualização parcial de um cliente (PATCH)
/// </summary>
public record PatchClienteCommand : IMediatorCommand<ClienteDto>
{
    /// <summary>
    /// ID do cliente a ser atualizado
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Nome do cliente (opcional - se null, não atualiza)
    /// </summary>
    public string? Nome { get; init; }

    /// <summary>
    /// CPF do cliente (opcional - se null, não atualiza)
    /// </summary>
    public string? Cpf { get; init; }

    /// <summary>
    /// E-mail do cliente (opcional - se null, não atualiza)
    /// </summary>
    public string? Email { get; init; }
}
