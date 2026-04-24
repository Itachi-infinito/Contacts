using SQLite;

namespace SparkWork2.Models;

public class JobOffer
{
    [PrimaryKey, AutoIncrement]
    public int JobOfferId { get; set; }

    public int RecruiterUserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int RecruiterId { get; set; }
}