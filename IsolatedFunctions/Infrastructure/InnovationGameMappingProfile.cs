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
        CreateMap<int?, int>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<string?, string>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<string?, Guid>().ConvertUsing(s => String.IsNullOrWhiteSpace(s) ? Guid.Empty : Guid.Parse(s));
        CreateMap<string, Guid>().ConvertUsing(s => Guid.Parse(s));

        CreateMap<User, User>();
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
            .ForMember(t => t.Id, opt =>
            {
                opt.PreCondition(s => s.Id != "");
                opt.MapFrom(s => Guid.Parse(s.Id));
            })
            .ForMember(dest => dest.Name, opt =>
            {
                opt.PreCondition(s => s.Username != "");
                opt.MapFrom(src => src.Username);
            })
            .ForMember(dest => dest.Password, opt =>
            {
                opt.PreCondition(s => s.Password != "");
                opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password));
            })
            ;


        CreateMap<CreateCardDto, Card>();

        CreateMap<Card, CardDto>()
            .ForMember(dest => dest.CardName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CardBody, opt => opt.MapFrom(src => src.Body))
            // .ReverseMap()
            ;

        CreateMap<GameSession, LobbyResponseDto>();

        CreateMap<GameSession, SessionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            ;
        CreateMap<SessionResponse, SessionResponseDto>();
    }
}
