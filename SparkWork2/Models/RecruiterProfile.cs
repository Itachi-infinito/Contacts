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

    public string Website { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public bool IsProfileVisible { get; set; } = true;
    public bool ShowSector { get; set; } = true;
    public bool ShowLocation { get; set; } = false;

}