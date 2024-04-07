using AutoMapper;
using Dapper;
using NewHRProject.Dto;
using NewHRProject.Models;
using System.Data;
using System.Data.SqlClient;

namespace NewHRProject.Services
{
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

        public async Task UploadLeaderboardData()
        {

        }
        public async Task UploadUserData(List<UserDataDto> input)
        {
            var data = _mapper.Map<List<User>>(input);
            data.ForEach(x => x.CreationDate = DateTime.Now);
            using (var conn =  new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                await conn.ExecuteAsync("insert into Users (UserName, FirstName, LastName, CreationDate) values(@UserName, @FirstName, @LastName, @CreationDate)", data);
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
                await conn.ExecuteAsync("insert into UserScores (UserId, Score, CreationDate) values(@UserId, @Score, @CreationDate)", input);
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
                var userScores = await conn.QueryAsync<UserScore>(@$"select * from UserScores 
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
                                        Users u
                                        LEFT JOIN
                                        UserScores us ON u.Id = us.UserId
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
            return null;
        }
        public async Task<List<ScoresByDayResponse>> GetAllData()
        {
            List<ScoresByDayResponse> result = new List<ScoresByDayResponse>();
            using (var conn = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                var userScores = await conn.QueryAsync<ScoresByDayResponse>(@$"select Users.UserName, UserScore.UserId, UserScore.Score 
                                        from UserScores
                                        left join Users ON UserScores.UserId = Users.Id");
                result = _mapper.Map<List<ScoresByDayResponse>>(userScores);
            }
            //არ აქვს პარამეტრები და აბრუნებს ყველა მონაცემს(ბაზაში არსებულ ყველა
            //ჩანაწერს) აჯამვის გარეშე, დაბრუნებული ობიექტი უნდა შეიცავდეს ინფორმაციას:
            //UserID, UserName, Score.რესპონსის ტიპი -ჯეისონი.
            return result;
        }
        public async Task GetStats()
        {
            var result = new GetStatsDto();
            //არ აქვს პარამეტრები და აბრუნებს ექვს მონაცემს:
            //    • საშუალო ყოველდღიური
            //    • საშუალო ყოველთვიური ქულა
            //    • მაქსიმალური ყოველდღიური ქულა
            //    • მაქსიმალური ყოველკვირეული ქულა
            //    • მაქსიმალური ყოველთვიური ქულა რესპონსის ტიპი - ჯეისონი.
        }
        public async Task<UserInfoDto> GetUserInfo(int userId)
        {
            string procedureName = "GetUserRank";
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(procedureName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("UserId", userId);

                    var row = await cmd.ExecuteNonQueryAsync();
                }
            }
            //გადაეცემა იუზერის ID და აბრუნებს თვის ჭრილში ქულების მიხედვით
            //მერამდენე ადგილზეა იუზერი და რამდენი ქულა აქვს.
            return null;
        }
    }
}
