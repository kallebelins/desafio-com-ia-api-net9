using AutoMapper;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.Commands.Cliente;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Application.Exceptions;
using ClienteEntity = DesafioComIA.Domain.Entities.Cliente;

namespace DesafioComIA.Application.Commands.Cliente;

public class CreateClienteCommandHandler : IMediatorCommandHandler<CreateClienteCommand, ClienteDto>
{
    private readonly IRepositoryAsync<ClienteEntity> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IMapper _mapper;

    public CreateClienteCommandHandler(
        IRepositoryAsync<ClienteEntity> repository,
        IUnitOfWorkAsync unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ClienteDto> Handle(CreateClienteCommand request, CancellationToken cancellationToken)
    {
        // Criar instâncias de ValueObjects (já validam e normalizam)
        var cpf = Cpf.Create(request.Cpf);
        var email = Email.Create(request.Email);

        // Validar se CPF já existe
        var clienteComCpf = await _repository.GetByAnyAsync(c => c.Cpf.Value == cpf.Value, cancellationToken);
        if (clienteComCpf)
        {
            throw new ClienteJaExisteException($"Já existe um cliente cadastrado com o CPF {cpf.Formatted}.");
        }

        // Validar se Email já existe
        var clienteComEmail = await _repository.GetByAnyAsync(c => c.Email.Value == email.Value, cancellationToken);
        if (clienteComEmail)
        {
            throw new ClienteJaExisteException($"Já existe um cliente cadastrado com o e-mail {email.Value}.");
        }

        // Criar nova instância de Cliente
        var cliente = new ClienteEntity(request.Nome, cpf, email);

        // Adicionar ao repositório
        await _repository.AddAsync(cliente, cancellationToken);

        // Salvar mudanças com UnitOfWork
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mapear para DTO e retornar
        return _mapper.Map<ClienteDto>(cliente);
    }
}
