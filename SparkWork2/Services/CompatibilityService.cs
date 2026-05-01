using SparkWork2.Models;

namespace SparkWork2.Services;

public class CompatibilityService
{
    public int CalculateScore(CandidateProfile candidate, JobOffer jobOffer)
    {
        double score = 0;
        double maxScore = 0;

        AddRequiredSkillsScore(candidate, jobOffer, ref score, ref maxScore);
        AddNiceToHaveSkillsScore(candidate, jobOffer, ref score, ref maxScore);
        AddContractScore(candidate, jobOffer, ref score, ref maxScore);
        AddLevelScore(candidate, jobOffer, ref score, ref maxScore);
        AddSalaryScore(candidate, jobOffer, ref score, ref maxScore);

        if (maxScore <= 0)
            return 0;

        return (int)Math.Round((score / maxScore) * 100);
    }

    public List<string> GetMatchedRequiredSkills(CandidateProfile candidate, JobOffer jobOffer)
    {
        var candidateSkills = SplitValues(candidate.Skills);
        var requiredSkills = SplitValues(jobOffer.RequiredSkills);

        return requiredSkills
            .Where(skill => candidateSkills.Contains(skill))
            .ToList();
    }

    public List<string> GetMissingRequiredSkills(CandidateProfile candidate, JobOffer jobOffer)
    {
        var candidateSkills = SplitValues(candidate.Skills);
        var requiredSkills = SplitValues(jobOffer.RequiredSkills);

        return requiredSkills
            .Where(skill => !candidateSkills.Contains(skill))
            .ToList();
    }

    public List<string> GetMatchedNiceToHaveSkills(CandidateProfile candidate, JobOffer jobOffer)
    {
        var candidateSkills = SplitValues(candidate.Skills);
        var niceSkills = SplitValues(jobOffer.NiceToHaveSkills);

        return niceSkills
            .Where(skill => candidateSkills.Contains(skill))
            .ToList();
    }

    private void AddRequiredSkillsScore(
        CandidateProfile candidate,
        JobOffer jobOffer,
        ref double score,
        ref double maxScore)
    {
        var candidateSkills = SplitValues(candidate.Skills);
        var requiredSkills = SplitValues(jobOffer.RequiredSkills);

        if (!requiredSkills.Any())
            return;

        maxScore += 50;

        int matchedCount = requiredSkills.Count(skill => candidateSkills.Contains(skill));
        score += 50.0 * matchedCount / requiredSkills.Count;
    }

    private void AddNiceToHaveSkillsScore(
        CandidateProfile candidate,
        JobOffer jobOffer,
        ref double score,
        ref double maxScore)
    {
        var candidateSkills = SplitValues(candidate.Skills);
        var niceSkills = SplitValues(jobOffer.NiceToHaveSkills);

        if (!niceSkills.Any())
            return;

        maxScore += 15;

        int matchedCount = niceSkills.Count(skill => candidateSkills.Contains(skill));
        score += 15.0 * matchedCount / niceSkills.Count;
    }

    private void AddContractScore(
        CandidateProfile candidate,
        JobOffer jobOffer,
        ref double score,
        ref double maxScore)
    {
        if (string.IsNullOrWhiteSpace(candidate.DesiredContractType) ||
            string.IsNullOrWhiteSpace(jobOffer.ContractType))
            return;

        maxScore += 15;

        if (Normalize(candidate.DesiredContractType).Contains(Normalize(jobOffer.ContractType)) ||
            Normalize(jobOffer.ContractType).Contains(Normalize(candidate.DesiredContractType)))
        {
            score += 15;
        }
    }

    private void AddLevelScore(
        CandidateProfile candidate,
        JobOffer jobOffer,
        ref double score,
        ref double maxScore)
    {
        if (string.IsNullOrWhiteSpace(candidate.ExperienceLevel) ||
            string.IsNullOrWhiteSpace(jobOffer.Level))
            return;

        maxScore += 10;

        if (Normalize(candidate.ExperienceLevel).Contains(Normalize(jobOffer.Level)) ||
            Normalize(jobOffer.Level).Contains(Normalize(candidate.ExperienceLevel)))
        {
            score += 10;
        }
    }

    private void AddSalaryScore(
        CandidateProfile candidate,
        JobOffer jobOffer,
        ref double score,
        ref double maxScore)
    {
        if (candidate.DesiredSalaryMin <= 0 && candidate.DesiredSalaryMax <= 0)
            return;

        if (jobOffer.SalaryMin <= 0 && jobOffer.SalaryMax <= 0)
            return;

        maxScore += 10;

        int candidateMin = candidate.DesiredSalaryMin;
        int candidateMax = candidate.DesiredSalaryMax > 0 ? candidate.DesiredSalaryMax : int.MaxValue;

        int offerMin = jobOffer.SalaryMin;
        int offerMax = jobOffer.SalaryMax > 0 ? jobOffer.SalaryMax : int.MaxValue;

        bool rangesOverlap = candidateMin <= offerMax && offerMin <= candidateMax;

        if (rangesOverlap)
            score += 10;
    }

    private List<string> SplitValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
    }

    private string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}

