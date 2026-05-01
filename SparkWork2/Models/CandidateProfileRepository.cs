using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class CandidateProfileRepository
{
    private readonly DatabaseService _databaseService;

    public CandidateProfileRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<CandidateProfile> GetCandidateProfileAsync(int candidateId, string fullName, string email)
    {
        var db = await _databaseService.GetConnectionAsync();

        var profile = await db.Table<CandidateProfile>()
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (profile != null)
            return profile;

        profile = new CandidateProfile
        {
            CandidateId = candidateId,
            FullName = fullName,
            Title = "",
            Location = "",
            About = "",
            Email = email,
            Skills = "",
            DesiredContractType = "",
            ExperienceLevel = "",
            DesiredSalaryMin = 0,
            DesiredSalaryMax = 0,
            MaxDistanceKm = 25,
            Latitude = 0,
            Longitude = 0
        };


        await db.InsertAsync(profile);
        return profile;
    }

    public async Task UpdateCandidateProfileAsync(CandidateProfile updatedProfile)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingProfile = await db.Table<CandidateProfile>()
            .FirstOrDefaultAsync(x => x.CandidateId == updatedProfile.CandidateId);

        if (existingProfile == null)
            await db.InsertAsync(updatedProfile);
        else
            await db.UpdateAsync(updatedProfile);
    }

    public async Task<List<CandidateProfile>> GetAllCandidateProfilesAsync()
    {
        var db = await _databaseService.GetConnectionAsync();
        return await db.Table<CandidateProfile>().ToListAsync();
    }

    public async Task<CandidateProfile?> GetByCandidateIdAsync(int candidateId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<CandidateProfile>()
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);
    }

    public async Task DeleteByCandidateIdAsync(int candidateId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var profile = await db.Table<CandidateProfile>()
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

        if (profile != null)
            await db.DeleteAsync(profile);
    }
}