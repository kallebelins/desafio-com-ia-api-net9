using FluentValidation;
using Mvp24Hours.Core.ValueObjects;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Validador para GetClientesQuery
/// </summary>
public class GetClientesQueryValidator : AbstractValidator<GetClientesQuery>
{
    public GetClientesQueryValidator()
    {
        // Validação de paginação
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("A página deve ser maior que 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("O tamanho da página deve estar entre 1 e 100.");

        // Validação de CPF (se informado)
        RuleFor(x => x.Cpf)
            .Must(cpf =>
            {
                if (string.IsNullOrWhiteSpace(cpf))
                    return true;

                try
                {
                    Cpf.Create(cpf);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("CPF inválido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf));

        // Validação de Email (se informado)
        RuleFor(x => x.Email)
            .Must(email =>
            {
                if (string.IsNullOrWhiteSpace(email))
                    return true;

                try
                {
                    Email.Create(email);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Email inválido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        // Validação de SortBy
        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || 
                           sortBy.Equals("Nome", StringComparison.OrdinalIgnoreCase) ||
                           sortBy.Equals("Cpf", StringComparison.OrdinalIgnoreCase) ||
                           sortBy.Equals("Email", StringComparison.OrdinalIgnoreCase))
            .WithMessage("O campo de ordenação deve ser Nome, Cpf ou Email.")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
    }
}
