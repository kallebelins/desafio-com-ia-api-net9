using FluentValidation;
using Mvp24Hours.Core.ValueObjects;
using DesafioComIA.Application.Commands.Cliente;

namespace DesafioComIA.Application.Commands.Cliente;

public class CreateClienteCommandValidator : AbstractValidator<CreateClienteCommand>
{
    public CreateClienteCommandValidator()
    {
        // Validação do Nome
        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("O nome é obrigatório.")
            .MinimumLength(3)
            .WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(200)
            .WithMessage("O nome deve ter no máximo 200 caracteres.");

        // Validação do CPF usando ValueObject do Mvp24Hours
        RuleFor(x => x.Cpf)
            .NotEmpty()
            .WithMessage("O CPF é obrigatório.")
            .Must(cpf => Cpf.IsValid(cpf))
            .WithMessage("O CPF informado é inválido.");

        // Validação do Email usando ValueObject do Mvp24Hours
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("O e-mail é obrigatório.")
            .Must(email => Email.IsValid(email))
            .WithMessage("O e-mail informado é inválido.");
    }
}
