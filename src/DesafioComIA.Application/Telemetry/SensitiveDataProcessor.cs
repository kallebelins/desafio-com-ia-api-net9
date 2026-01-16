using System.Text.RegularExpressions;

namespace DesafioComIA.Application.Telemetry;

/// <summary>
/// Processador para mascarar dados sensíveis em logs e traces.
/// </summary>
public static partial class SensitiveDataProcessor
{
    /// <summary>
    /// Mascara um CPF (ex: 123.456.789-00 → ***.456.789-**).
    /// </summary>
    /// <param name="cpf">CPF a ser mascarado.</param>
    /// <returns>CPF mascarado ou valor original se inválido.</returns>
    public static string MaskCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return "***";

        // Remove formatação
        var cleanCpf = CpfDigitsRegex().Replace(cpf, "");

        if (cleanCpf.Length != 11)
            return "***";

        // Mascara: mostra apenas os dígitos do meio
        return $"***.{cleanCpf.Substring(3, 3)}.{cleanCpf.Substring(6, 3)}-**";
    }

    /// <summary>
    /// Mascara um email (ex: user@example.com → u***@example.com).
    /// </summary>
    /// <param name="email">Email a ser mascarado.</param>
    /// <returns>Email mascarado ou valor original se inválido.</returns>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "***@***.***";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***@***.***";

        var localPart = email[..atIndex];
        var domainPart = email[(atIndex + 1)..];

        // Mostra apenas o primeiro caractere do local part
        var maskedLocal = localPart.Length > 0
            ? $"{localPart[0]}***"
            : "***";

        return $"{maskedLocal}@{domainPart}";
    }

    /// <summary>
    /// Mascara um valor genérico baseado no nome do campo.
    /// </summary>
    /// <param name="fieldName">Nome do campo.</param>
    /// <param name="value">Valor a ser mascarado.</param>
    /// <returns>Valor mascarado se for dado sensível, ou o valor original.</returns>
    public static string? MaskIfSensitive(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var lowerFieldName = fieldName.ToLowerInvariant();

        if (lowerFieldName.Contains("cpf"))
            return MaskCpf(value);

        if (lowerFieldName.Contains("email"))
            return MaskEmail(value);

        if (lowerFieldName.Contains("password") || lowerFieldName.Contains("senha"))
            return "***";

        if (lowerFieldName.Contains("token") || lowerFieldName.Contains("secret"))
            return "***";

        return value;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex CpfDigitsRegex();
}
