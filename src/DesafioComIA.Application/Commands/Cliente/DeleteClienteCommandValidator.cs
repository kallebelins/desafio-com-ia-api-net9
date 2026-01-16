using FluentValidation;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Validador para DeleteClienteCommand
/// </summary>
public class DeleteClienteCommandValidator : AbstractValidator<DeleteClienteCommand>
{
    public DeleteClienteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("O ID do cliente é obrigatório.")
            .NotEqual(Guid.Empty)
            .WithMessage("O ID do cliente informado é inválido.");
    }
}
