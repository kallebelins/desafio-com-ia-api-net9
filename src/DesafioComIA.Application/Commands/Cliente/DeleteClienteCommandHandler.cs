using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.Exceptions;
using DesafioComIA.Application.Telemetry;
using DesafioComIA.Infrastructure.Services.Cache;
using ClienteEntity = DesafioComIA.Domain.Entities.Cliente;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Handler para remoção de um cliente
/// </summary>
public class DeleteClienteCommandHandler : IMediatorCommandHandler<DeleteClienteCommand, bool>
{
    private readonly IRepositoryAsync<ClienteEntity> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DeleteClienteCommandHandler> _logger;
    private readonly ClienteMetrics _metrics;

    public DeleteClienteCommandHandler(
        IRepositoryAsync<ClienteEntity> repository,
        IUnitOfWorkAsync unitOfWork,
        ICacheService cacheService,
        ILogger<DeleteClienteCommandHandler> logger,
        ClienteMetrics metrics)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool> Handle(DeleteClienteCommand request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DeleteCliente");
        activity?.SetClienteId(request.Id);

        var stopwatch = Stopwatch.StartNew();
        var sucesso = false;

        try
        {
            activity?.AddEvent(new ActivityEvent("BuscandoCliente"));

            // Buscar cliente existente
            var cliente = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (cliente is null)
            {
                throw new ClienteNaoEncontradoException(
                    $"Cliente com ID '{request.Id}' não foi encontrado.",
                    new Dictionary<string, object> { { "ClienteId", request.Id } });
            }

            activity?.AddEvent(new ActivityEvent("RemovendoCliente"));

            // Remover cliente
            await _repository.RemoveAsync(cliente, cancellationToken);

            // Salvar mudanças com UnitOfWork
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.AddEvent(new ActivityEvent("ClienteRemovido"));

            // Invalidar cache
            await InvalidateCacheAsync(request.Id, cancellationToken);

            sucesso = true;
            activity?.SetSuccess("Cliente removido com sucesso");
            _metrics.ClienteRemovido();

            return true;
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RegistrarTempoProcessamento(stopwatch.ElapsedMilliseconds, "DeleteCliente", sucesso);
        }
    }

    /// <summary>
    /// Invalida o cache do cliente e das listagens/buscas
    /// </summary>
    private async Task InvalidateCacheAsync(Guid clienteId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Invalidando cache do cliente {ClienteId} após deleção", clienteId);

            // Invalidar cache específico do cliente
            await _cacheService.RemoveAsync(CacheKeyHelper.GetClienteByIdKey(clienteId), cancellationToken);

            // Invalidar cache de listagem
            await _cacheService.RemoveByPatternAsync(CacheKeyHelper.GetClientesListPattern(), cancellationToken);

            // Invalidar cache de busca
            await _cacheService.RemoveByPatternAsync(CacheKeyHelper.GetClientesSearchPattern(), cancellationToken);

            _logger.LogDebug("Cache do cliente {ClienteId} invalidado com sucesso", clienteId);
        }
        catch (Exception ex)
        {
            // Não falhar a operação se a invalidação do cache falhar
            _logger.LogWarning(ex, "Erro ao invalidar cache do cliente {ClienteId} após deleção", clienteId);
        }
    }
}
