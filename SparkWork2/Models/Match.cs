using SQLite;

namespace SparkWork2.Models;

public class Match
{
    [PrimaryKey, AutoIncrement]
    public int MatchId { get; set; }

    public int UserId { get; set; }
    public int CandidateUserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;

    public int RecruiterUserId { get; set; }

    public int JobOfferId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;

    public bool ShowToCandidate { get; set; }
    public bool ShowToRecruiter { get; set; }
}