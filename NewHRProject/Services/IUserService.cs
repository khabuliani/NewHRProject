using NewHRProject.Dto;

namespace NewHRProject.Services;

public interface IUserService
{
    public Task UploadUserScores(List<UserScoresDto> input);
    public Task UploadUserData(List<UserDataDto> input);
    public Task<List<ScoresByDayResponse>> GetScoresByDay(DateTime date);
    public Task<List<ScoresByDayResponse>> GeScoresByMonth(DateTime date);
    public Task<List<ScoresByDayResponse>> GetAllData();
    public Task GetStats();
    public Task<UserInfoDto> GetUserInfo(int userId);
}
