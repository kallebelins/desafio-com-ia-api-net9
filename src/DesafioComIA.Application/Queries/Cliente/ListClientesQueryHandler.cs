using AutoMapper;
using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Core.ValueObjects.Logic;
using DesafioComIA.Application.DTOs;
using System.Linq.Expressions;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Handler para listar clientes com paginação e ordenação
/// </summary>
public class ListClientesQueryHandler : IMediatorQueryHandler<ListClientesQuery, PagedResult<ClienteListDto>>
{
    private readonly IRepositoryAsync<Domain.Entities.Cliente> _repository;
    private readonly IMapper _mapper;

    public ListClientesQueryHandler(IRepositoryAsync<Domain.Entities.Cliente> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ClienteListDto>> Handle(ListClientesQuery request, CancellationToken cancellationToken)
    {
        // Calcular offset
        var offset = (request.Page - 1) * request.PageSize;

        // Criar critérios de paginação com expressão para suportar ordenação
        var pagingCriteria = new PagingCriteriaExpression<Domain.Entities.Cliente>(request.PageSize, offset, null, null);

        // Configurar ordenação
        if (request.Descending)
        {
            switch (request.SortBy?.ToLower())
            {
                case "cpf":
                    pagingCriteria.OrderByDescendingExpr.Add(c => c.Cpf);
                    break;
                case "email":
                    pagingCriteria.OrderByDescendingExpr.Add(c => c.Email);
                    break;
                default:
                    pagingCriteria.OrderByDescendingExpr.Add(c => c.Nome);
                    break;
            }
        }
        else
        {
            switch (request.SortBy?.ToLower())
            {
                case "cpf":
                    pagingCriteria.OrderByAscendingExpr.Add(c => c.Cpf);
                    break;
                case "email":
                    pagingCriteria.OrderByAscendingExpr.Add(c => c.Email);
                    break;
                default:
                    pagingCriteria.OrderByAscendingExpr.Add(c => c.Nome);
                    break;
            }
        }

        // Buscar clientes com paginação
        var clientes = await _repository.ListAsync(pagingCriteria, cancellationToken);

        // Contar total de registros
        var totalCount = await _repository.ListCountAsync(cancellationToken);

        // Mapear para DTOs
        var clienteDtos = _mapper.Map<IList<ClienteListDto>>(clientes).ToList();

        // Retornar resultado paginado
        return new PagedResult<ClienteListDto>(
            items: clienteDtos,
            currentPage: request.Page,
            pageSize: request.PageSize,
            totalCount: totalCount
        );
    }
}
