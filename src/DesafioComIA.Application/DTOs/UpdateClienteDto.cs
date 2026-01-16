using System.ComponentModel.DataAnnotations;

namespace DesafioComIA.Application.DTOs;

/// <summary>
/// DTO para atualização completa de um cliente (PUT)
/// </summary>
public class UpdateClienteDto
{
    /// <summary>
    /// Nome do cliente
    /// </summary>
    /// <example>João da Silva</example>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MinLength(3, ErrorMessage = "O nome deve ter no mínimo 3 caracteres.")]
    [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// CPF do cliente (com ou sem formatação)
    /// </summary>
    /// <example>123.456.789-00</example>
    [Required(ErrorMessage = "O CPF é obrigatório.")]
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// E-mail do cliente
    /// </summary>
    /// <example>joao.silva@example.com</example>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail informado é inválido.")]
    public string Email { get; set; } = string.Empty;
}
