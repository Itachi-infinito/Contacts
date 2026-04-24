using SQLite;

namespace SparkWork2.Models;

public class CandidateJobLike
{
    [PrimaryKey, AutoIncrement]
    public int LikeId { get; set; }

    public int CandidateUserId { get; set; }
    public int JobOfferId { get; set; }
}