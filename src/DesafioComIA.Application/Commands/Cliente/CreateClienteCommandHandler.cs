using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.Commands.Cliente;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Application.Exceptions;
using DesafioComIA.Application.Telemetry;
using DesafioComIA.Infrastructure.Services.Cache;
using ClienteEntity = DesafioComIA.Domain.Entities.Cliente;

namespace DesafioComIA.Application.Commands.Cliente;

public class CreateClienteCommandHandler : IMediatorCommandHandler<CreateClienteCommand, ClienteDto>
{
    private readonly IRepositoryAsync<ClienteEntity> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateClienteCommandHandler> _logger;
    private readonly ClienteMetrics _metrics;

    public CreateClienteCommandHandler(
        IRepositoryAsync<ClienteEntity> repository,
        IUnitOfWorkAsync unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<CreateClienteCommandHandler> logger,
        ClienteMetrics metrics)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<ClienteDto> Handle(CreateClienteCommand request, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("CreateCliente");
        activity?.SetClienteTag(request.Nome, request.Cpf, request.Email);

        var stopwatch = Stopwatch.StartNew();
        var sucesso = false;

        try
        {
            activity?.AddEvent(new ActivityEvent("ValidandoValueObjects"));

            // Criar instâncias de ValueObjects (já validam e normalizam)
            var cpf = Cpf.Create(request.Cpf);
            var email = Email.Create(request.Email);

            activity?.AddEvent(new ActivityEvent("VerificandoDuplicidade"));

            // Validar se CPF já existe
            var clienteComCpf = await _repository.GetByAnyAsync(c => c.Cpf == cpf, cancellationToken);
            if (clienteComCpf)
            {
                throw new ClienteJaExisteException($"Já existe um cliente cadastrado com o CPF {cpf.Formatted}.");
            }

            // Validar se Email já existe
            var clienteComEmail = await _repository.GetByAnyAsync(c => c.Email == email, cancellationToken);
            if (clienteComEmail)
            {
                throw new ClienteJaExisteException($"Já existe um cliente cadastrado com o e-mail {email.Value}.");
            }

            activity?.AddEvent(new ActivityEvent("CriandoCliente"));

            // Criar nova instância de Cliente
            var cliente = new ClienteEntity(request.Nome, cpf, email);

            // Adicionar ao repositório
            await _repository.AddAsync(cliente, cancellationToken);

            // Salvar mudanças com UnitOfWork
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetClienteId(cliente.Id);
            activity?.AddEvent(new ActivityEvent("ClienteCriado"));

            // Invalidar cache de listagens e buscas
            await InvalidateCacheAsync(cancellationToken);

            sucesso = true;
            activity?.SetSuccess("Cliente criado com sucesso");
            _metrics.ClienteCriado();

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
            _metrics.RegistrarTempoProcessamento(stopwatch.ElapsedMilliseconds, "CreateCliente", sucesso);
        }
    }

    /// <summary>
    /// Invalida o cache de listagens e buscas de clientes
    /// </summary>
    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Invalidando cache de clientes após criação");

            // Invalidar cache de listagem
            await _cacheService.RemoveByPatternAsync(CacheKeyHelper.GetClientesListPattern(), cancellationToken);

            // Invalidar cache de busca
            await _cacheService.RemoveByPatternAsync(CacheKeyHelper.GetClientesSearchPattern(), cancellationToken);

            _logger.LogDebug("Cache de clientes invalidado com sucesso");
        }
        catch (Exception ex)
        {
            // Não falhar a operação se a invalidação do cache falhar
            _logger.LogWarning(ex, "Erro ao invalidar cache de clientes após criação");
        }
    }
}
