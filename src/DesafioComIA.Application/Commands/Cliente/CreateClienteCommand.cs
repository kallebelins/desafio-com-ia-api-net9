using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Application.Commands.Cliente;

public record CreateClienteCommand : IMediatorCommand<ClienteDto>
{
    public string Nome { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
