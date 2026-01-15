using AutoMapper;
using DesafioComIA.Application.DTOs;
using DesafioComIA.Domain.Entities;
using Mvp24Hours.Core.ValueObjects;

namespace DesafioComIA.Application.Mappings;

public class ClienteProfile : Profile
{
    public ClienteProfile()
    {
        // Mapeamento Cliente → ClienteDto
        CreateMap<Cliente, ClienteDto>()
            .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.Cpf.Value))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value));

        // Mapeamento Cliente → ClienteListDto
        CreateMap<Cliente, ClienteListDto>()
            .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.Cpf.Value))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value));

        // Mapeamento CreateClienteDto → Cliente
        CreateMap<CreateClienteDto, Cliente>()
            .ConstructUsing(src => new Cliente(
                src.Nome,
                Cpf.Create(src.Cpf),
                Email.Create(src.Email)
            ))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore());
    }
}
