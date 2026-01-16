using AutoMapper;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Application.Exceptions;
using ClienteEntity = DesafioComIA.Domain.Entities.Cliente;

namespace DesafioComIA.Application.Commands.Cliente;

/// <summary>
/// Handler para atualização parcial de um cliente (PATCH)
/// </summary>
public class PatchClienteCommandHandler : IMediatorCommandHandler<PatchClienteCommand, ClienteDto>
{
    private readonly IRepositoryAsync<ClienteEntity> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IMapper _mapper;

    public PatchClienteCommandHandler(
        IRepositoryAsync<ClienteEntity> repository,
        IUnitOfWorkAsync unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ClienteDto> Handle(PatchClienteCommand request, CancellationToken cancellationToken)
    {
        // Buscar cliente existente
        var cliente = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (cliente is null)
        {
            throw new ClienteNaoEncontradoException(
                $"Cliente com ID '{request.Id}' não foi encontrado.",
                new Dictionary<string, object> { { "ClienteId", request.Id } });
        }

        // Atualizar Nome se informado
        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            cliente.AtualizarNome(request.Nome);
        }

        // Atualizar CPF se informado
        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpf = Cpf.Create(request.Cpf);

            // Validar se novo CPF já existe em outro cliente
            var clienteComCpf = await _repository.GetByAsync(c => c.Cpf == cpf && c.Id != request.Id, cancellationToken);
            if (clienteComCpf?.Any() == true)
            {
                throw new ClienteJaExisteException($"Já existe outro cliente cadastrado com o CPF {cpf.Formatted}.");
            }

            cliente.AtualizarCpf(cpf);
        }

        // Atualizar Email se informado
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = Email.Create(request.Email);

            // Validar se novo Email já existe em outro cliente
            var clienteComEmail = await _repository.GetByAsync(c => c.Email == email && c.Id != request.Id, cancellationToken);
            if (clienteComEmail?.Any() == true)
            {
                throw new ClienteJaExisteException($"Já existe outro cliente cadastrado com o e-mail {email.Value}.");
            }

            cliente.AtualizarEmail(email);
        }

        // Atualizar no repositório
        await _repository.ModifyAsync(cliente, cancellationToken);

        // Salvar mudanças com UnitOfWork
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mapear para DTO e retornar
        return _mapper.Map<ClienteDto>(cliente);
    }
}
