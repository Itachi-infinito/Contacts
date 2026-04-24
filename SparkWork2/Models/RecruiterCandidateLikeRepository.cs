using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class RecruiterCandidateLikeRepository
{
    private readonly DatabaseService _databaseService;

    public RecruiterCandidateLikeRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<bool> AddLikeAsync(int recruiterUserId, int candidateUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingLike = await db.Table<RecruiterCandidateLike>()
            .FirstOrDefaultAsync(x =>
                x.RecruiterUserId == recruiterUserId &&
                x.CandidateUserId == candidateUserId);

        if (existingLike != null)
            return false;

        var like = new RecruiterCandidateLike
        {
            RecruiterUserId = recruiterUserId,
            CandidateUserId = candidateUserId
        };

        await db.InsertAsync(like);
        return true;
    }

    public async Task<List<RecruiterCandidateLike>> GetLikesByRecruiterAsync(int recruiterUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<RecruiterCandidateLike>()
            .Where(x => x.RecruiterUserId == recruiterUserId)
            .ToListAsync();
    }

    public async Task<bool> IsLikedAsync(int recruiterUserId, int candidateUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingLike = await db.Table<RecruiterCandidateLike>()
            .FirstOrDefaultAsync(x =>
                x.RecruiterUserId == recruiterUserId &&
                x.CandidateUserId == candidateUserId);

        return existingLike != null;
    }

    public async Task<bool> HasRecruiterLikedCandidateAsync(int recruiterUserId, int candidateUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingLike = await db.Table<RecruiterCandidateLike>()
            .FirstOrDefaultAsync(x =>
                x.RecruiterUserId == recruiterUserId &&
                x.CandidateUserId == candidateUserId);

        return existingLike != null;
    }

    public async Task<List<RecruiterCandidateLike>> GetAllLikesAsync()
    {
        var db = await _databaseService.GetConnectionAsync();
        return await db.Table<RecruiterCandidateLike>().ToListAsync();
    }

    public async Task DeleteLikeAsync(int recruiterUserId, int candidateUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var like = await db.Table<RecruiterCandidateLike>()
            .FirstOrDefaultAsync(x =>
                x.RecruiterUserId == recruiterUserId &&
                x.CandidateUserId == candidateUserId);

        if (like != null)
            await db.DeleteAsync(like);
    }
}