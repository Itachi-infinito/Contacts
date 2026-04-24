namespace SparkWork2.Models;

public class RecruiterMatchItem
{
    public int MatchId { get; set; }
    public int CandidateUserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public int JobOfferId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
}