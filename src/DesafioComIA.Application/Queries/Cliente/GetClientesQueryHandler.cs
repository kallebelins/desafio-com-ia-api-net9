using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.Logic.Pagination;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Core.ValueObjects.Logic;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Application.Telemetry;
using DesafioComIA.Infrastructure.Configuration;
using DesafioComIA.Infrastructure.Services.Cache;
using System.Linq.Expressions;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Handler para buscar clientes com filtros, paginação e ordenação
/// </summary>
public class GetClientesQueryHandler : IMediatorQueryHandler<GetClientesQuery, PagedResult<ClienteListDto>>
{
    private readonly IRepositoryAsync<Domain.Entities.Cliente> _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<GetClientesQueryHandler> _logger;
    private readonly ClienteMetrics _metrics;

    public GetClientesQueryHandler(
        IRepositoryAsync<Domain.Entities.Cliente> repository,
        IMapper mapper,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<GetClientesQueryHandler> logger,
        ClienteMetrics metrics)
    {
        _repository = repository;
        _mapper = mapper;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<PagedResult<ClienteListDto>> Handle(GetClientesQuery request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("SearchClientes");
        activity?.SetTag("query.page", request.Page);
        activity?.SetTag("query.page_size", request.PageSize);
        activity?.SetTag("query.has_nome_filter", !string.IsNullOrEmpty(request.Nome));
        activity?.SetTag("query.has_cpf_filter", !string.IsNullOrEmpty(request.Cpf));
        activity?.SetTag("query.has_email_filter", !string.IsNullOrEmpty(request.Email));

        var stopwatch = Stopwatch.StartNew();
        var sucesso = false;

        try
        {
            // Gerar chave de cache
            var cacheKey = CacheKeyHelper.GetSearchClientesKey(
                request.Nome,
                request.Cpf,
                request.Email,
                request.Page,
                request.PageSize,
                request.SortBy,
                request.Descending);

            // Usar cache com GetOrCreateAsync
            var result = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async ct => await FetchFromDatabaseAsync(request, ct),
                TimeSpan.FromMinutes(_cacheSettings.SearchClientesTTLMinutes),
                cancellationToken);

            sucesso = true;
            activity?.SetTag("query.total_count", result.TotalCount);
            activity?.SetTag("query.items_returned", result.Items.Count);
            activity?.SetSuccess();
            _metrics.BuscaRealizada();

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RegistrarTempoProcessamento(stopwatch.ElapsedMilliseconds, "SearchClientes", sucesso);
        }
    }

    /// <summary>
    /// Busca os dados diretamente do banco de dados
    /// </summary>
    private async Task<PagedResult<ClienteListDto>> FetchFromDatabaseAsync(GetClientesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Cache miss para busca de clientes - buscando do banco de dados");

        // Construir expressão de filtro dinâmica
        Expression<Func<Domain.Entities.Cliente, bool>>? filter = BuildFilterExpression(request);

        // Calcular offset
        var offset = (request.Page - 1) * request.PageSize;

        // Criar critérios de paginação com expressão
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

        // Buscar clientes com filtros e paginação
        var clientes = filter != null
            ? await _repository.GetByAsync(filter, pagingCriteria, cancellationToken)
            : await _repository.ListAsync(pagingCriteria, cancellationToken);

        // Contar total de registros filtrados
        var totalCount = filter != null
            ? await _repository.GetByCountAsync(filter, cancellationToken)
            : await _repository.ListCountAsync(cancellationToken);

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

    /// <summary>
    /// Constrói a expressão de filtro dinâmica baseada nos parâmetros da query
    /// </summary>
    private Expression<Func<Domain.Entities.Cliente, bool>>? BuildFilterExpression(GetClientesQuery request)
    {
        Expression<Func<Domain.Entities.Cliente, bool>>? filter = null;

        // Filtro por Nome (parcial, case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            var nomeNormalizado = request.Nome.Trim();
            Expression<Func<Domain.Entities.Cliente, bool>> nomeFilter = 
                c => c.Nome.ToLower().Contains(nomeNormalizado.ToLower());
            filter = CombineFilters(filter, nomeFilter);
        }

        // Filtro por CPF (exato)
        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpfValueObject = Cpf.Create(request.Cpf);
            Expression<Func<Domain.Entities.Cliente, bool>> cpfFilter = 
                c => c.Cpf == cpfValueObject;
            filter = CombineFilters(filter, cpfFilter);
        }

        // Filtro por Email (exato, case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailValueObject = Email.Create(request.Email);
            Expression<Func<Domain.Entities.Cliente, bool>> emailFilter = 
                c => c.Email == emailValueObject;
            filter = CombineFilters(filter, emailFilter);
        }

        return filter;
    }

    /// <summary>
    /// Combina duas expressões de filtro com operador AND
    /// </summary>
    private Expression<Func<Domain.Entities.Cliente, bool>>? CombineFilters(
        Expression<Func<Domain.Entities.Cliente, bool>>? filter1,
        Expression<Func<Domain.Entities.Cliente, bool>> filter2)
    {
        if (filter1 == null)
            return filter2;

        // Combinar com AND usando Expression.AndAlso
        var parameter = Expression.Parameter(typeof(Domain.Entities.Cliente), "c");
        var body1 = Expression.Invoke(filter1, parameter);
        var body2 = Expression.Invoke(filter2, parameter);
        var combinedBody = Expression.AndAlso(body1, body2);

        return Expression.Lambda<Func<Domain.Entities.Cliente, bool>>(combinedBody, parameter);
    }
}
