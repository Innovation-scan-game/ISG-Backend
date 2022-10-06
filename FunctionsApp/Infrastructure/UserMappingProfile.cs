using System;
using AutoMapper;
using Domain.Models;
using FunctionsApp.DTO.UserDTOs;

namespace FunctionsApp.Infrastructure;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserDTO, User>();
        CreateMap<CreateUserDTO, User>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Password, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
            ;
        CreateMap<User, UserDTO>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Name))
            .ReverseMap();

        CreateMap<EditUserDTO, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            ;
    }
}
