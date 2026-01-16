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
/// Handler para listar clientes com paginação e ordenação
/// </summary>
public class ListClientesQueryHandler : IMediatorQueryHandler<ListClientesQuery, PagedResult<ClienteListDto>>
{
    private readonly IRepositoryAsync<Domain.Entities.Cliente> _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<ListClientesQueryHandler> _logger;
    private readonly ClienteMetrics _metrics;

    public ListClientesQueryHandler(
        IRepositoryAsync<Domain.Entities.Cliente> repository,
        IMapper mapper,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<ListClientesQueryHandler> logger,
        ClienteMetrics metrics)
    {
        _repository = repository;
        _mapper = mapper;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<PagedResult<ClienteListDto>> Handle(ListClientesQuery request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ListClientes");
        activity?.SetTag("query.page", request.Page);
        activity?.SetTag("query.page_size", request.PageSize);
        activity?.SetTag("query.sort_by", request.SortBy ?? "nome");
        activity?.SetTag("query.descending", request.Descending);

        var stopwatch = Stopwatch.StartNew();
        var sucesso = false;

        try
        {
            // Gerar chave de cache
            var cacheKey = CacheKeyHelper.GetListClientesKey(
                request.Page,
                request.PageSize,
                request.SortBy,
                request.Descending);

            // Usar cache com GetOrCreateAsync
            var result = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async ct => await FetchFromDatabaseAsync(request, ct),
                TimeSpan.FromMinutes(_cacheSettings.ListClientesTTLMinutes),
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
            _metrics.RegistrarTempoProcessamento(stopwatch.ElapsedMilliseconds, "ListClientes", sucesso);
        }
    }

    /// <summary>
    /// Busca os dados diretamente do banco de dados
    /// </summary>
    private async Task<PagedResult<ClienteListDto>> FetchFromDatabaseAsync(ListClientesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Cache miss para listagem de clientes - buscando do banco de dados");

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
