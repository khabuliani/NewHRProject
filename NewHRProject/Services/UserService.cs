using AutoMapper;
using Dapper;
using NewHRProject.Dto;
using NewHRProject.Models;
using System.Data;
using System.Data.SqlClient;

namespace NewHRProject.Services;

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public UserService(
        IConfiguration configuration,
        IMapper mapper)
    {
        _configuration = configuration;
        _mapper = mapper;
    }


    public async Task UploadUserData(List<UserDataDto> input)
    {
        var data = _mapper.Map<List<User>>(input);
        var userNames = input.Select(x => x.UserName).ToList();
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var userScores = await conn.QueryAsync<User>(@$"SELECT * FROM [dbo].[Users] WHERE UserName IN @Usernames;" ,
                new { Usernames = userNames });
            if (userScores.Any())
            {
                throw new Exception("Users with this UserNames already exist: " + string.Join(",", userScores));
            }
        }
        data.ForEach(x => x.CreationDate = DateTime.Now);
        using (var conn =  new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await conn.ExecuteAsync("insert into [dbo].[Users] (UserName, FirstName, LastName, CreationDate) values(@UserName, @FirstName, @LastName, @CreationDate)", data);
        }
        //იღებს იუზერების ობიექტების მასივს, იუზერის ობიექტს უნდა ქონდეს
        //3 ველი, იუზერის სახელი, იუზერის გვარი და იუზერნეიმი(უნდა იყოს უნიკალური).
        //მაგალითი:
        //UserFirstname_1
        //UserLastName_1
        //UserName_111
        //იუზერების დამატების შემთხვევაში ბაზაში შენახული უნდა იყოს იუზერის დამატების
        //თარიღი და იუზერის უნიკალური ID, იუზერნეიმის გამეორების შემთხვევაში უნდა
        //გამოვიდეს შესაბამისი ერორ მესიჯი.
    }
    public async Task UploadUserScores(List<UserScoresDto> input)
    {
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await conn.ExecuteAsync("insert into [dbo].[UserScores] (UserId, Score, CreationDate) values(@UserId, @Score, @CreationDate)", input);
        }
        //სერვისის მიზანია შევინახოთ იუზერების ქულები დღეების მიხედვით.
        //პარამეტრად იღებს იუზერების ქულების ობიექტების მასივს. ობიექტს უნდა ქონდეს 3
        //ველი იუზერის უნიკალური ID, თარიღი და ქულა, მაგალითი:
        //UserID, Date, Score.
    }
    public async Task<List<ScoresByDayResponse>> GetScoresByDay(DateTime date)
    {
        List<ScoresByDayResponse> result = new List<ScoresByDayResponse>();
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var userScores = await conn.QueryAsync<UserScore>(@$"select * from [dbo].[UserScores] 
                                        where CreationDate >= @StartDate 
                                        and CreationDate <= @EndDate 
                                        group by UserId 
                                        order by Score", 
                                    new { StartDate = date, EndDate = date.AddDays(1)});
            result = _mapper.Map<List<ScoresByDayResponse>>(userScores);
        }
        //პარამეტრად იღებს დღეს და ამ დღეზე არსებულ ინფორმაციას აბრუნებს
        //დასორტირებულს ქულების ჯამის მიხედვით, და დაგრუპულს იუზერის მიხედვით,
        //დაბრუნებული ობიექტი უნდა შეიცავდეს ინფორმაციას: UserID, UserName, Score.
        //რესპონსის ტიპი -ჯეისონი.
        return result;
    }
    public async Task<List<ScoresByDayResponse>> GeScoresByMonth(DateTime date)
    {
        var endDate = date.AddMonths(1);
        List<ScoresByDayResponse> result = new List<ScoresByDayResponse>();
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            var userScores = await conn.QueryAsync<ScoresByDayResponse>(@$"SELECT
                                        u.UserId,
                                        u.UserName,
                                        SUM(us.Score) AS Score
                                        FROM
                                        [dbo].[Users] u
                                        LEFT JOIN
                                        [dbo].[UserScores] us ON u.Id = us.UserId
                                        where us.CreationDate >= @StartDate 
                                        and us.CreationDate <= @EndDate",
                                    new { StartDate = date, endDate });
            result = userScores.ToList();
        }
        //პარამეტრად იღებს თვეს და ამ თვეში არსებული იუზერების ჯამურ
        //ინფორმაციას აბრუნებს, დასორტირებულს ქულების ჯამის, დაგრუპულს იუზერის
        //მიხედვით.თუ ერთ დღეს იუზერს ჰქონდა
        //20 ქულა და მეორე დღეს 40 ქულა, ეს მეთოდი დააბრუნებს 60 - ს ამ იუზერზე,
        //დაბრუნებული ობიექტი უნდა შეიცავდეს ინფორმაციას: UserID, UserName, Score.
        //რესპონსის ტიპი -ჯეისონი.
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
        //არ აქვს პარამეტრები და აბრუნებს ყველა მონაცემს(ბაზაში არსებულ ყველა
        //ჩანაწერს) აჯამვის გარეშე, დაბრუნებული ობიექტი უნდა შეიცავდეს ინფორმაციას:
        //UserID, UserName, Score.რესპონსის ტიპი -ჯეისონი.
        return result;
    }

    public async Task GetStats()
    {
        var result = new GetStatsDto();
        result.DailyAverage = DailyAverage();
        result.MonthlyAverage = MonthlyAverage();
        result.MaximumDaily = MaximumDaily();
        result.MaximumWeekly = MaximumWeekly();
        result.MaximumMonthly = MaximumMonthly();
        //არ აქვს პარამეტრები და აბრუნებს ექვს მონაცემს:
        //    • საშუალო ყოველდღიური
        //    • საშუალო ყოველთვიური ქულა
        //    • მაქსიმალური ყოველდღიური ქულა
        //    • მაქსიმალური ყოველკვირეული ქულა
        //    • მაქსიმალური ყოველთვიური ქულა რესპონსის ტიპი - ჯეისონი.
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
            //გადაეცემა იუზერის ID და აბრუნებს თვის ჭრილში ქულების მიხედვით
            //მერამდენე ადგილზეა იუზერი და რამდენი ქულა აქვს.
            return result;
        }
    }

    private double DailyAverage()
    {
        double result = 0;  
        using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            //var userScores = await conn.QueryAsync<ScoreDto>(@$"SELECT
            //                    DATE(CreationDate) AS Date,
            //                    AVG(Score) AS TotalScore
            //                    FROM [dbo].[UserScores]
            //                    GROUP BY DATE(CreationDate);");
            var sql = @$"SELECT
                                DATE(CreationDate) AS Date,
                                AVG(Score) AS TotalScore
                                FROM [dbo].[UserScores]
                                GROUP BY DATE(CreationDate);";
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
            //var userScores = await conn.QueryAsync<ScoreDto>(@$"SELECT
            //                    YEAR(CreationDate) AS Year,
            //                    MONTH(CreationDate) AS Month,
            //                    AVG(Score) AS TotalScore
            //                    FROM [dbo].[UserScores]
            //                    GROUP BY YEAR(CreationDate), MONTH(CreationDate);");
            var sql = @$"SELECT
                                YEAR(CreationDate) AS Year,
                                MONTH(CreationDate) AS Month,
                                AVG(Score) AS TotalScore
                                FROM [dbo].[UserScores]
                                GROUP BY YEAR(CreationDate), MONTH(CreationDate);";
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
            //var userScores = await conn.QueryAsync<ScoreDto>(@$"SELECT
            //                    DATE(CreationDate) AS Date,
            //                    MAX(Score) AS TotalScore
            //                    FROM [dbo].[UserScores]
            //                    GROUP BY DATE(CreationDate);");
            var sql = @$"SELECT
                                DATE(CreationDate) AS Date,
                                MAX(Score) AS TotalScore
                                FROM [dbo].[UserScores]
                                GROUP BY DATE(CreationDate);";
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
            //var userScores = await conn.QueryAsync<ScoreDto>(@$"SELECT
            //                    YEAR(CreationDate) AS Year,
            //                    WEEK(CreationDate) AS Week,
            //                    MAX(Score) AS TotalScore
            //                    FROM [dbo].[UserScores]
            //                    GROUP BY YEAR(CreationDate), WEEK(CreationDate);");
            var sql = @$"SELECT
                                YEAR(CreationDate) AS Year,
                                WEEK(CreationDate) AS Week,
                                MAX(Score) AS TotalScore
                                FROM [dbo].[UserScores]
                                GROUP BY YEAR(CreationDate), WEEK(CreationDate);";
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
            //var userScores = await conn.QueryAsync<ScoreDto>(@$"SELECT
            //                    YEAR(CreationDate) AS Year,
            //                    MONTH(CreationDate) AS Month,
            //                    MAX(Score) AS TotalScore
            //                    FROM [dbo].[UserScores]
            //                    GROUP BY YEAR(CreationDate), MONTH(CreationDate);");
            var sql = @$"SELECT
                                YEAR(CreationDate) AS Year,
                                MONTH(CreationDate) AS Month,
                                MAX(Score) AS TotalScore
                                FROM [dbo].[UserScores]
                                GROUP BY YEAR(CreationDate), MONTH(CreationDate);";
            var a = conn.Query<ScoreDto>(sql).ToList();
            result = a.First().TotalScore;
        }
        return result;
    }
}
