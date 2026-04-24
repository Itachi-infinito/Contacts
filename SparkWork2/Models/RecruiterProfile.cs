using SQLite;

namespace SparkWork2.Models;

public class RecruiterProfile
{
    [PrimaryKey]
    public int RecruiterId { get; set; }

    public string CompanyName { get; set; }
    public string Sector { get; set; }
    public string Location { get; set; }
    public string ContactEmail { get; set; }
    public string Description { get; set; }
    public string? CompanyPhotoPath { get; set; }
}