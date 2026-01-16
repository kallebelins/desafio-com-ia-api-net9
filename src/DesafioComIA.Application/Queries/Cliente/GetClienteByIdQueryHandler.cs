using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Application.Exceptions;
using DesafioComIA.Application.Telemetry;
using DesafioComIA.Infrastructure.Configuration;
using DesafioComIA.Infrastructure.Services.Cache;
using ClienteEntity = DesafioComIA.Domain.Entities.Cliente;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Handler para buscar um cliente específico por ID
/// </summary>
public class GetClienteByIdQueryHandler : IMediatorQueryHandler<GetClienteByIdQuery, ClienteDto>
{
    private readonly IRepositoryAsync<ClienteEntity> _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<GetClienteByIdQueryHandler> _logger;
    private readonly ClienteMetrics _metrics;

    public GetClienteByIdQueryHandler(
        IRepositoryAsync<ClienteEntity> repository,
        IMapper mapper,
        ICacheService cacheService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<GetClienteByIdQueryHandler> logger,
        ClienteMetrics metrics)
    {
        _repository = repository;
        _mapper = mapper;
        _cacheService = cacheService;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<ClienteDto> Handle(GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("GetClienteById");
        activity?.SetClienteId(request.Id);

        var stopwatch = Stopwatch.StartNew();
        var sucesso = false;

        try
        {
            // Gerar chave de cache
            var cacheKey = CacheKeyHelper.GetClienteByIdKey(request.Id);

            // Usar cache com GetOrCreateAsync
            var result = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async ct => await FetchFromDatabaseAsync(request.Id, ct),
                TimeSpan.FromMinutes(_cacheSettings.GetClienteByIdTTLMinutes),
                cancellationToken);

            sucesso = true;
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
            _metrics.RegistrarTempoProcessamento(stopwatch.ElapsedMilliseconds, "GetClienteById", sucesso);
        }
    }

    /// <summary>
    /// Busca o cliente diretamente do banco de dados
    /// </summary>
    private async Task<ClienteDto> FetchFromDatabaseAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Cache miss para cliente {ClienteId} - buscando do banco de dados", id);

        // Buscar cliente por Id
        var cliente = await _repository.GetByIdAsync(id, cancellationToken);

        // Se não encontrado, lançar exceção
        if (cliente is null)
        {
            throw new ClienteNaoEncontradoException(
                $"Cliente com ID '{id}' não foi encontrado.",
                new Dictionary<string, object> { { "ClienteId", id } });
        }

        // Mapear para DTO e retornar
        return _mapper.Map<ClienteDto>(cliente);
    }
}
