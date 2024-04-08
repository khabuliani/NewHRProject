using Dapper;
using NewHRProject.Dto;
using NewHRProject.Models;
using System.Data;
using System.Data.SqlClient;

namespace NewHRProject.Services;

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;

    public UserService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task UploadUserData(List<UserDataDto> input)
    {
        var selected = from a in  input
                   select new User
                   {
                       UserName = a.UserName,
                       LastName = a.LastName,
                       FirstName = a.Firstname
                   };
        var data = selected.ToList();
        var userNames = input.Select(x => x.UserName).ToList();
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var userScores = await conn.QueryAsync<User>(@$"SELECT * FROM [dbo].[Users] WHERE UserName IN @Usernames;" ,
                new { Usernames = userNames });
            if (userScores.Any())
            {
                throw new Exception("Users with this UserNames already exist: " + string.Join(",", userScores.Select(x => x.UserName)));
            }
        }
        data.ForEach(x => x.CreationDate = DateTime.Now);
        using (var conn =  new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await conn.ExecuteAsync("insert into [dbo].[Users] (UserName, FirstName, LastName, CreationDate) values(@UserName, @FirstName, @LastName, @CreationDate)", data);
        }
    }

    public async Task UploadUserScores(List<UserScoresDto> input)
    {
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await conn.ExecuteAsync("insert into [dbo].[UserScores] (UserId, Score, CreationDate) values(@UserId, @Score, @CreationDate)", input);
        }
    }

    public async Task<List<ScoresByDayResponse>> GetScoresByDay(DateTime date)
    {
        List<ScoresByDayResponse> result = new List<ScoresByDayResponse>();
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var userScores = await conn.QueryAsync<ScoresByDayResponse>(@$"SELECT 
                                        U.Id AS UserId, U.UserName, SUM(US.Score) AS Score
                                        FROM [dbo].[Users] U
                                        JOIN [dbo].[UserScores] US ON U.Id = US.UserId
                                        where US.CreationDate >= @StartDate 
                                        and US.CreationDate <= @EndDate
                                        GROUP BY U.Id, U.UserName", 
                                    new { StartDate = date, EndDate = date.AddDays(1)});
            result = userScores.ToList();
        }
        return result;
    }

    //ეს ეიპიაი თვის პირველ რიცხვს იღებს
    public async Task<List<ScoresByDayResponse>> GeScoresByMonth(DateTime date)
    {
        var endDate = date.AddMonths(1);
        List<ScoresByDayResponse> result = new List<ScoresByDayResponse>();
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var userScores = await conn.QueryAsync<ScoresByDayResponse>(@$"SELECT 
                                        U.Id AS UserId, U.UserName, SUM(US.Score) AS Score
                                        FROM [dbo].[Users] U
                                        JOIN [dbo].[UserScores] US ON U.Id = US.UserId
                                        where US.CreationDate >= @StartDate 
                                        and US.CreationDate <= @EndDate
                                        GROUP BY U.Id, U.UserName",
                                    new { StartDate = date, EndDate =  endDate });
            result = userScores.ToList();
        }
        return result;
    }
    public async Task<List<ScoresByDayResponse>> GetAllData()
    {
        List<ScoresByDayResponse> result = new List<ScoresByDayResponse>();
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var userScores = await conn.QueryAsync<ScoresByDayResponse>(@$"select [dbo].[Users].[UserName], [dbo].[UserScores].[UserId], [dbo].[UserScores].[Score] 
                                        from [dbo].[UserScores]
                                        left join [dbo].[Users] ON [dbo].[UserScores].[UserId] = [dbo].[Users].[Id]");
            result = userScores.ToList();
        }
        return result;
    }

    //აქ ვერ მივხვდი კომპანიის ჭრილში უნდა დამეთვალა თუ ერთი თანამშრომელის ჭრილში და მეორე ვარიანტიც გავაკეთე
    public async Task<GetStatsDto> GetStats()
    {
        var result = new GetStatsDto();
        result.DailyAverage = DailyAverage();
        result.MonthlyAverage = MonthlyAverage();
        result.MaximumDaily = MaximumDaily();
        result.MaximumWeekly = MaximumWeekly();
        result.MaximumMonthly = MaximumMonthly();
        return result;
    }

    //ერთი თანამშრომელის ჭრილში
    public async Task<GetStatsDto> GetStatsForOneUser()
    {
        var result = new GetStatsDto();
        result.DailyAverage = DailyAverageForOneUser();
        result.MonthlyAverage = MonthlyAverageForOneUser();
        result.MaximumDaily = MaximumDailyForOneUser();
        result.MaximumWeekly = MaximumWeeklyForOneUser();
        result.MaximumMonthly = MaximumMonthlyForOneUser();
        return result;
    }

    public async Task<UserInfoDto> GetUserInfo(int userId)
    {
        var result = new UserInfoDto();
        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand("GetUserRankAndTotalScore", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@Rank", SqlDbType.Int).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@TotalScore", SqlDbType.Float).Direction = ParameterDirection.Output;
                cmd.ExecuteNonQuery();
                result.Place = (int)cmd.Parameters["@Rank"].Value;
                result.Score = (double)cmd.Parameters["@TotalScore"].Value;
            }
            return result;
        }
    }

    private double DailyAverage()
    {
        double result = 0;  
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT AVG(DailyScore) AS TotalScore
                    FROM (
                        SELECT CAST(CreationDate AS DATE) AS Date, SUM(Score) AS DailyScore
                        FROM [dbo].[UserScores]
                        GROUP BY CAST(CreationDate AS DATE)
                    ) AS DailyScores;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MonthlyAverage()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT 
                        AVG(MonthlyScore) AS TotalScore
                        FROM (
                            SELECT 
                                YEAR(CreationDate) AS Year,
                                MONTH(CreationDate) AS Month,
                                SUM(Score) AS MonthlyScore
                            FROM [dbo].[UserScores]
                            GROUP BY YEAR(CreationDate), MONTH(CreationDate)
                        ) AS MonthlyScores;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MaximumDaily()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT MAX(DailySum) AS TotalScore
                        FROM (
                            SELECT CAST(CreationDate AS DATE) AS Date, SUM(Score) AS DailySum
                            FROM [dbo].[UserScores]
                            GROUP BY CAST(CreationDate AS DATE)
                        ) AS DailyScores;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MaximumWeekly()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT MAX(WeeklySum) AS TotalScore
                        FROM (
                            SELECT 
                                DATEPART(YEAR, CreationDate) AS Year, 
                                DATEPART(WEEK, CreationDate) AS Week,
                                SUM(Score) AS WeeklySum
                            FROM [dbo].[UserScores]
                            GROUP BY DATEPART(YEAR, CreationDate), DATEPART(WEEK, CreationDate)
                        ) AS WeeklyScores;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MaximumMonthly()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT MAX(MonthlySum) AS TotalScore
                        FROM (
                            SELECT 
                                DATEPART(YEAR, CreationDate) AS Year, 
                                DATEPART(MONTH, CreationDate) AS Month,
                                SUM(Score) AS MonthlySum
                            FROM [dbo].[UserScores]
                            GROUP BY DATEPART(YEAR, CreationDate), DATEPART(MONTH, CreationDate)
                        ) AS MonthlyScores;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double DailyAverageForOneUser()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT AVG(DailyScore) AS TotalScore
                    FROM (
                        SELECT CAST(CreationDate AS DATE) AS Date, SUM(Score) AS DailyScore
                        FROM [dbo].[UserScores]
                        GROUP BY CAST(CreationDate AS DATE), (UserId)
                    ) AS DailyScores;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MonthlyAverageForOneUser()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT AVG(MonthlySum) AS TotalScore
                        FROM (
                            SELECT YEAR(CreationDate) AS Year, MONTH(CreationDate) AS Month, SUM(Score) AS MonthlySum
                            FROM [dbo].[UserScores]
                            GROUP BY YEAR(CreationDate), MONTH(CreationDate), (UserId)
                        ) AS MonthlyScores;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MaximumDailyForOneUser()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT MAX(MaxDailySum) AS TotalScore
                        FROM (
                            SELECT 
                                CAST(CreationDate AS DATE) AS Date,
                                SUM(Score) AS MaxDailySum
                            FROM [dbo].[UserScores]
                            GROUP BY UserId, CAST(CreationDate AS DATE)
                        ) AS DailySums;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MaximumWeeklyForOneUser()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT MAX(MaxWeeklySum) AS TotalScore
                        FROM (
                            SELECT 
                                DATEPART(YEAR, CreationDate) AS Year,
                                DATEPART(WEEK, CreationDate) AS Week,
                                SUM(Score) AS MaxWeeklySum
                            FROM [dbo].[UserScores]
                            GROUP BY UserId, DATEPART(YEAR, CreationDate), DATEPART(WEEK, CreationDate)
                        ) AS WeeklySums;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }

    private double MaximumMonthlyForOneUser()
    {
        double result = 0;
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var sql = @$"SELECT MAX(MaxMonthlySum) AS TotalScore
                        FROM (
                            SELECT 
                                DATEPART(YEAR, CreationDate) AS Year,
                                DATEPART(MONTH, CreationDate) AS Month,
                                SUM(Score) AS MaxMonthlySum
                            FROM [dbo].[UserScores]
                            GROUP BY UserId, DATEPART(YEAR, CreationDate), DATEPART(MONTH, CreationDate)
                        ) AS MonthlySums;";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }
}
