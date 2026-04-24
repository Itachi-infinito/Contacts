using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class RecruiterProfileRepository
{
    private readonly DatabaseService _databaseService;

    public RecruiterProfileRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<RecruiterProfile> GetRecruiterProfileAsync(int recruiterId, string companyName, string email)
    {
        var db = await _databaseService.GetConnectionAsync();

        var profile = await db.Table<RecruiterProfile>()
            .FirstOrDefaultAsync(x => x.RecruiterId == recruiterId);

        if (profile != null)
            return profile;

        profile = new RecruiterProfile
        {
            RecruiterId = recruiterId,
            CompanyName = companyName,
            Sector = string.Empty,
            Location = string.Empty,
            ContactEmail = email,
            Description = string.Empty,
            CompanyPhotoPath = string.Empty
        };

        await db.InsertAsync(profile);
        return profile;
    }

    public async Task UpdateRecruiterProfileAsync(RecruiterProfile updatedProfile)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingProfile = await db.Table<RecruiterProfile>()
            .FirstOrDefaultAsync(x => x.RecruiterId == updatedProfile.RecruiterId);

        if (existingProfile == null)
            await db.InsertAsync(updatedProfile);
        else
            await db.UpdateAsync(updatedProfile);
    }

    public async Task<List<RecruiterProfile>> GetAllRecruiterProfilesAsync()
    {
        var db = await _databaseService.GetConnectionAsync();
        return await db.Table<RecruiterProfile>().ToListAsync();
    }

    public async Task<RecruiterProfile?> GetByRecruiterIdAsync(int recruiterId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<RecruiterProfile>()
            .FirstOrDefaultAsync(x => x.RecruiterId == recruiterId);
    }

    public async Task DeleteByRecruiterIdAsync(int recruiterId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var profile = await db.Table<RecruiterProfile>()
            .FirstOrDefaultAsync(x => x.RecruiterId == recruiterId);

        if (profile != null)
            await db.DeleteAsync(profile);
    }
}