using AutoMapper;
using Domain.Models;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.DTO.GameSessionDTOs;
using IsolatedFunctions.DTO.UserDTOs;

namespace IsolatedFunctions.Infrastructure;

public class InnovationGameMappingProfile : Profile
{
    public InnovationGameMappingProfile()
    {
        CreateMap<UserDto, User>();
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Password, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
            ;
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Name))
            .ReverseMap();

        CreateMap<User, LobbyPlayerDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Name))
            .ReverseMap();


        CreateMap<EditUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            ;

        CreateMap<CreateCardDto, Card>();

        CreateMap<Card, CardDto>()
            .ForMember(dest => dest.CardName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CardBody, opt => opt.MapFrom(src => src.Body))
            // .ReverseMap()
            ;

        CreateMap<GameSession, LobbyResponseDto>();

    }
}
