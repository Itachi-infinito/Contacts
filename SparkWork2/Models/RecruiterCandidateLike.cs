using SQLite;

namespace SparkWork2.Models;

public class RecruiterCandidateLike
{
    [PrimaryKey, AutoIncrement]
    public int RecruiterCandidateLikeId { get; set; }

    public int RecruiterUserId { get; set; }
    public int CandidateUserId { get; set; }

    public bool IsSuperLike { get; set; }
}
