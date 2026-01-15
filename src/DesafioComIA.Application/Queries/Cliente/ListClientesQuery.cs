using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Query para listar clientes com paginação e ordenação
/// </summary>
public record ListClientesQuery : IMediatorQuery<PagedResult<ClienteListDto>>
{
    /// <summary>
    /// Número da página (começando em 1)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Quantidade de itens por página (máximo 100)
    /// </summary>
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Campo para ordenação (padrão: Nome)
    /// </summary>
    public string SortBy { get; init; } = "Nome";

    /// <summary>
    /// Ordenação descendente (padrão: false - ascendente)
    /// </summary>
    public bool Descending { get; init; } = false;
}
