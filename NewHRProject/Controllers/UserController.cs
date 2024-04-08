using Microsoft.AspNetCore.Mvc;
using NewHRProject.Dto;
using NewHRProject.Services;
using System.Collections.Generic;

namespace NewHRProject.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task UploadUserData(List<UserDataDto> input)
    {
        await _userService.UploadUserData(input);
    }

    [HttpPost]
    public async Task UploadUserScores(List<UserScoresDto> input)
    {
        await _userService.UploadUserScores(input);
    }

    [HttpGet]
    public async Task<List<ScoresByDayResponse>> GetScoresByDay(DateTime date)
    {
        return await _userService.GetScoresByDay(date);
    }

    [HttpGet]
    public async Task<List<ScoresByDayResponse>> GeScoresByMonth(DateTime date)
    {
        return await _userService.GeScoresByMonth(date);
    }

    [HttpGet]
    public async Task<List<ScoresByDayResponse>> GetAllData()
    {
        return await _userService.GetAllData();
    }

    [HttpGet]
    public async Task<GetStatsDto> GetStats()
    {
        return await _userService.GetStats();
    }

    [HttpGet]
    public async Task<GetStatsDto> GetStatsForOneUser()
    {
        return await _userService.GetStatsForOneUser();
    }

    [HttpGet]
    public async Task<UserInfoDto> GetUserInfo(int userId)
    {
        return await _userService.GetUserInfo(userId);
    }
}
