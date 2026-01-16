using AutoMapper;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Application.Exceptions;
using ClienteEntity = DesafioComIA.Domain.Entities.Cliente;

namespace DesafioComIA.Application.Queries.Cliente;

/// <summary>
/// Handler para buscar um cliente específico por ID
/// </summary>
public class GetClienteByIdQueryHandler : IMediatorQueryHandler<GetClienteByIdQuery, ClienteDto>
{
    private readonly IRepositoryAsync<ClienteEntity> _repository;
    private readonly IMapper _mapper;

    public GetClienteByIdQueryHandler(IRepositoryAsync<ClienteEntity> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ClienteDto> Handle(GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        // Buscar cliente por Id
        var cliente = await _repository.GetByIdAsync(request.Id, cancellationToken);

        // Se não encontrado, lançar exceção
        if (cliente is null)
        {
            throw new ClienteNaoEncontradoException(
                $"Cliente com ID '{request.Id}' não foi encontrado.",
                new Dictionary<string, object> { { "ClienteId", request.Id } });
        }

        // Mapear para DTO e retornar
        return _mapper.Map<ClienteDto>(cliente);
    }
}
