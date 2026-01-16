using FluentValidation;
using Mvp24Hours.Core.ValueObjects;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Validador para PatchClienteCommand
/// </summary>
public class PatchClienteCommandValidator : AbstractValidator<PatchClienteCommand>
{
    public PatchClienteCommandValidator()
    {
        // Validação do Id
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("O ID do cliente é obrigatório.")
            .NotEqual(Guid.Empty)
            .WithMessage("O ID do cliente informado é inválido.");

        // Validação do Nome (se informado)
        RuleFor(x => x.Nome)
            .MinimumLength(3)
            .WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(200)
            .WithMessage("O nome deve ter no máximo 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Nome));

        // Validação do CPF (se informado) usando ValueObject do Mvp24Hours
        RuleFor(x => x.Cpf)
            .Must(cpf => Cpf.IsValid(cpf!))
            .WithMessage("O CPF informado é inválido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf));

        // Validação do Email (se informado) usando ValueObject do Mvp24Hours
        RuleFor(x => x.Email)
            .Must(email => Email.IsValid(email!))
            .WithMessage("O e-mail informado é inválido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        // Pelo menos um campo deve ser informado
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Nome) || 
                       !string.IsNullOrWhiteSpace(x.Cpf) || 
                       !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Pelo menos um campo (Nome, CPF ou E-mail) deve ser informado para atualização.");
    }
}
