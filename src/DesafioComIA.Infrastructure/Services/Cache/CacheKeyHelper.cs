using System.Security.Cryptography;
using System.Text;

namespace DesafioComIA.Infrastructure.Services.Cache;

/// <summary>
/// Helper para geração consistente de chaves de cache
/// </summary>
public static class CacheKeyHelper
{
    /// <summary>
    /// Prefixo base para todas as chaves de cache de clientes
    /// </summary>
    public const string ClientesPrefix = "clientes";

    /// <summary>
    /// Gera chave para listagem de clientes
    /// </summary>
    /// <param name="page">Número da página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="sortBy">Campo de ordenação</param>
    /// <param name="descending">Se ordenação é descendente</param>
    /// <returns>Chave de cache formatada</returns>
    public static string GetListClientesKey(int page, int pageSize, string? sortBy, bool descending)
    {
        var sort = string.IsNullOrWhiteSpace(sortBy) ? "nome" : sortBy.ToLowerInvariant();
        return $"{ClientesPrefix}:list:{page}:{pageSize}:{sort}:{(descending ? "desc" : "asc")}";
    }

    /// <summary>
    /// Gera chave para busca/search de clientes (usa hash para evitar chaves longas)
    /// </summary>
    /// <param name="nome">Filtro de nome (opcional)</param>
    /// <param name="cpf">Filtro de CPF (opcional)</param>
    /// <param name="email">Filtro de email (opcional)</param>
    /// <param name="page">Número da página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="sortBy">Campo de ordenação</param>
    /// <param name="descending">Se ordenação é descendente</param>
    /// <returns>Chave de cache formatada</returns>
    public static string GetSearchClientesKey(
        string? nome,
        string? cpf,
        string? email,
        int page,
        int pageSize,
        string? sortBy,
        bool descending)
    {
        var sort = string.IsNullOrWhiteSpace(sortBy) ? "nome" : sortBy.ToLowerInvariant();
        var filterString = $"nome:{nome ?? "null"}|cpf:{cpf ?? "null"}|email:{email ?? "null"}|page:{page}|size:{pageSize}|sort:{sort}|desc:{descending}";
        var hash = ComputeHash(filterString);
        return $"{ClientesPrefix}:search:{hash}";
    }

    /// <summary>
    /// Gera chave para buscar cliente por ID
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <returns>Chave de cache formatada</returns>
    public static string GetClienteByIdKey(Guid id)
    {
        return $"{ClientesPrefix}:id:{id}";
    }

    /// <summary>
    /// Obtém padrão para invalidar todas as listagens de clientes
    /// </summary>
    /// <returns>Padrão para matching</returns>
    public static string GetClientesListPattern()
    {
        return $"{ClientesPrefix}:list:*";
    }

    /// <summary>
    /// Obtém padrão para invalidar todas as buscas de clientes
    /// </summary>
    /// <returns>Padrão para matching</returns>
    public static string GetClientesSearchPattern()
    {
        return $"{ClientesPrefix}:search:*";
    }

    /// <summary>
    /// Obtém padrão para invalidar todo o cache de clientes
    /// </summary>
    /// <returns>Padrão para matching</returns>
    public static string GetClientesPattern()
    {
        return $"{ClientesPrefix}:*";
    }

    /// <summary>
    /// Computa hash MD5 de uma string para criar chaves mais curtas
    /// </summary>
    /// <param name="input">String de entrada</param>
    /// <returns>Hash hexadecimal da string</returns>
    private static string ComputeHash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
