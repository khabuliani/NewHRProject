namespace NewHRProject.Models;

public class UserScore
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public double Score { get; set; }
    public DateTime CreationDate { get; set; }
}
