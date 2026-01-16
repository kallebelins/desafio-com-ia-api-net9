using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.Exceptions;
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

    public DeleteClienteCommandHandler(
        IRepositoryAsync<ClienteEntity> repository,
        IUnitOfWorkAsync unitOfWork,
        ICacheService cacheService,
        ILogger<DeleteClienteCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteClienteCommand request, CancellationToken cancellationToken)
    {
        // Buscar cliente existente
        var cliente = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (cliente is null)
        {
            throw new ClienteNaoEncontradoException(
                $"Cliente com ID '{request.Id}' não foi encontrado.",
                new Dictionary<string, object> { { "ClienteId", request.Id } });
        }

        // Remover cliente
        await _repository.RemoveAsync(cliente, cancellationToken);

        // Salvar mudanças com UnitOfWork
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidar cache
        await InvalidateCacheAsync(request.Id, cancellationToken);

        return true;
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
