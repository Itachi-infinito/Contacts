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

    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public int SalaryMin { get; set; }
    public int SalaryMax { get; set; }

    public string RequiredSkills { get; set; } = string.Empty;
    public string NiceToHaveSkills { get; set; } = string.Empty;

    public string RemoteMode { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;

    [Ignore]
    public bool HasSalary => SalaryMin > 0 || SalaryMax > 0;

    [Ignore]
    public string SalaryDisplay
    {
        get
        {
            if (SalaryMin > 0 && SalaryMax > 0)
                return $"{SalaryMin} - {SalaryMax} €";

            if (SalaryMin > 0)
                return $"À partir de {SalaryMin} €";

            if (SalaryMax > 0)
                return $"Jusqu'à {SalaryMax} €";

            return "Salaire non renseigné";
        }
    }

    [Ignore]
    public bool HasRequiredSkills => !string.IsNullOrWhiteSpace(RequiredSkills);

    [Ignore]
    public bool HasNiceToHaveSkills => !string.IsNullOrWhiteSpace(NiceToHaveSkills);

    [Ignore]
    public bool HasLevel => !string.IsNullOrWhiteSpace(Level);

    [Ignore]
    public bool HasRemoteMode => !string.IsNullOrWhiteSpace(RemoteMode);

}
