using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Application.Exceptions;
using DesafioComIA.Application.Telemetry;
using DesafioComIA.Infrastructure.Services.Cache;
using ClienteEntity = DesafioComIA.Domain.Entities.Cliente;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Handler para atualização completa de um cliente (PUT)
/// </summary>
public class UpdateClienteCommandHandler : IMediatorCommandHandler<UpdateClienteCommand, ClienteDto>
{
    private readonly IRepositoryAsync<ClienteEntity> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UpdateClienteCommandHandler> _logger;
    private readonly ClienteMetrics _metrics;

    public UpdateClienteCommandHandler(
        IRepositoryAsync<ClienteEntity> repository,
        IUnitOfWorkAsync unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<UpdateClienteCommandHandler> logger,
        ClienteMetrics metrics)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<ClienteDto> Handle(UpdateClienteCommand request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("UpdateCliente");
        activity?.SetClienteId(request.Id);
        activity?.SetClienteTag(request.Nome, request.Cpf, request.Email);

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

            activity?.AddEvent(new ActivityEvent("ValidandoValueObjects"));

            // Criar instâncias de ValueObjects
            var cpf = Cpf.Create(request.Cpf);
            var email = Email.Create(request.Email);

            activity?.AddEvent(new ActivityEvent("VerificandoDuplicidade"));

            // Validar se novo CPF já existe em outro cliente
            var clienteComCpf = await _repository.GetByAsync(c => c.Cpf == cpf && c.Id != request.Id, cancellationToken);
            if (clienteComCpf?.Any() == true)
            {
                throw new ClienteJaExisteException($"Já existe outro cliente cadastrado com o CPF {cpf.Formatted}.");
            }

            // Validar se novo Email já existe em outro cliente
            var clienteComEmail = await _repository.GetByAsync(c => c.Email == email && c.Id != request.Id, cancellationToken);
            if (clienteComEmail?.Any() == true)
            {
                throw new ClienteJaExisteException($"Já existe outro cliente cadastrado com o e-mail {email.Value}.");
            }

            activity?.AddEvent(new ActivityEvent("AtualizandoCliente"));

            // Atualizar todas as propriedades do cliente
            cliente.AtualizarNome(request.Nome);
            cliente.AtualizarCpf(cpf);
            cliente.AtualizarEmail(email);

            // Atualizar no repositório
            await _repository.ModifyAsync(cliente, cancellationToken);

            // Salvar mudanças com UnitOfWork
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.AddEvent(new ActivityEvent("ClienteAtualizado"));

            // Invalidar cache
            await InvalidateCacheAsync(request.Id, cancellationToken);

            sucesso = true;
            activity?.SetSuccess("Cliente atualizado com sucesso");
            _metrics.ClienteAtualizado();

            // Mapear para DTO e retornar
            return _mapper.Map<ClienteDto>(cliente);
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RegistrarTempoProcessamento(stopwatch.ElapsedMilliseconds, "UpdateCliente", sucesso);
        }
    }

    /// <summary>
    /// Invalida o cache do cliente e das listagens/buscas
    /// </summary>
    private async Task InvalidateCacheAsync(Guid clienteId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Invalidando cache do cliente {ClienteId} após atualização", clienteId);

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
            _logger.LogWarning(ex, "Erro ao invalidar cache do cliente {ClienteId} após atualização", clienteId);
        }
    }
}
