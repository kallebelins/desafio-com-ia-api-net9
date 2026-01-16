using FluentValidation;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Validador para GetClienteByIdQuery
/// </summary>
public class GetClienteByIdQueryValidator : AbstractValidator<GetClienteByIdQuery>
{
    public GetClienteByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("O ID do cliente é obrigatório.")
            .NotEqual(Guid.Empty)
            .WithMessage("O ID do cliente informado é inválido.");
    }
}
