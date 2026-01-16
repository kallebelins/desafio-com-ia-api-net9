using System.ComponentModel.DataAnnotations;

namespace DesafioComIA.Application.DTOs;

/// <summary>
/// DTO para atualização parcial de um cliente (PATCH)
/// </summary>
public class PatchClienteDto
{
    /// <summary>
    /// Nome do cliente (opcional)
    /// </summary>
    /// <example>João da Silva</example>
    [MinLength(3, ErrorMessage = "O nome deve ter no mínimo 3 caracteres.")]
    [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string? Nome { get; set; }

    /// <summary>
    /// CPF do cliente (opcional, com ou sem formatação)
    /// </summary>
    /// <example>123.456.789-00</example>
    public string? Cpf { get; set; }

    /// <summary>
    /// E-mail do cliente (opcional)
    /// </summary>
    /// <example>joao.silva@example.com</example>
    [EmailAddress(ErrorMessage = "O e-mail informado é inválido.")]
    public string? Email { get; set; }
}
