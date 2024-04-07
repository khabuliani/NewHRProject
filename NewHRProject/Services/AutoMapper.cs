using AutoMapper;
using NewHRProject.Dto;
using NewHRProject.Models;

namespace NewHRProject.Services;

public class AutoMapper : Profile
{
    protected AutoMapper()
    {
        CreateMap<User, UserDataDto>().ReverseMap();
        CreateMap<UserScore, ScoresByDayResponse>().ReverseMap();
    }
}
