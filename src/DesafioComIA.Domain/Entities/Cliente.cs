using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.ValueObjects;

namespace DesafioComIA.Domain.Entities;

public class Cliente : EntityBase<Guid>
{
    public string Nome { get; private set; } = string.Empty;
    public Cpf Cpf { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Construtor padrão para EF Core
    private Cliente()
    {
    }

    // Construtor com parâmetros principais
    public Cliente(string nome, Cpf cpf, Email email)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Cpf = cpf;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza o nome do cliente
    /// </summary>
    /// <param name="nome">Novo nome do cliente</param>
    public void AtualizarNome(string nome)
    {
        Nome = nome;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza o CPF do cliente
    /// </summary>
    /// <param name="cpf">Novo CPF do cliente</param>
    public void AtualizarCpf(Cpf cpf)
    {
        Cpf = cpf;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza o e-mail do cliente
    /// </summary>
    /// <param name="email">Novo e-mail do cliente</param>
    public void AtualizarEmail(Email email)
    {
        Email = email;
        ModifiedAt = DateTime.UtcNow;
    }
}
