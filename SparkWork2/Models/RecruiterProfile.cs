using SQLite;

namespace SparkWork2.Models;

public class RecruiterProfile
{
    [PrimaryKey]
    public int RecruiterId { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? CompanyPhotoPath { get; set; }
}