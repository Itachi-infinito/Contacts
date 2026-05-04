using SQLite;

namespace SparkWork2.Models;

public class CandidateProfile
{
    [PrimaryKey]
    public int CandidateId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }

    public string Skills { get; set; } = string.Empty;
    public string DesiredContractType { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;

    public int DesiredSalaryMin { get; set; }
    public int DesiredSalaryMax { get; set; }

    public int MaxDistanceKm { get; set; } = 25;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string ExperienceTitle1 { get; set; } = string.Empty;
    public string ExperienceCompany1 { get; set; } = string.Empty;
    public string ExperiencePeriod1 { get; set; } = string.Empty;

    public string ExperienceTitle2 { get; set; } = string.Empty;
    public string ExperienceCompany2 { get; set; } = string.Empty;
    public string ExperiencePeriod2 { get; set; } = string.Empty;
}
