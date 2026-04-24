namespace SparkWork2.Models;

public class CandidateBrowseItem
{
    public int CandidateUserId { get; set; }
    public string FullName { get; set; }
    public string Title { get; set; }
    public string Location { get; set; }
    public string About { get; set; }
    public string Email { get; set; }

    public bool IsAlreadyLiked { get; set; }
    public string LikeButtonText => IsAlreadyLiked ? "Already liked" : "Like";
}