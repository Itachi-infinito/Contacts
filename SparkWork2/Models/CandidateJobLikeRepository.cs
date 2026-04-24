using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class CandidateJobLikeRepository
{
    private readonly DatabaseService _databaseService;
    private readonly JobOfferRepository _jobOfferRepository;

    public CandidateJobLikeRepository(
        DatabaseService databaseService,
        JobOfferRepository jobOfferRepository)
    {
        _databaseService = databaseService;
        _jobOfferRepository = jobOfferRepository;
    }

    public async Task<bool> AddLikeAsync(int candidateUserId, int jobOfferId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingLike = await db.Table<CandidateJobLike>()
            .FirstOrDefaultAsync(x =>
                x.CandidateUserId == candidateUserId &&
                x.JobOfferId == jobOfferId);

        if (existingLike != null)
            return false;

        var like = new CandidateJobLike
        {
            CandidateUserId = candidateUserId,
            JobOfferId = jobOfferId
        };

        await db.InsertAsync(like);
        return true;
    }

    public async Task<bool> IsLikedAsync(int candidateUserId, int jobOfferId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingLike = await db.Table<CandidateJobLike>()
            .FirstOrDefaultAsync(x =>
                x.CandidateUserId == candidateUserId &&
                x.JobOfferId == jobOfferId);

        return existingLike != null;
    }

    public async Task<List<CandidateJobLike>> GetLikesByCandidateAsync(int candidateUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<CandidateJobLike>()
            .Where(x => x.CandidateUserId == candidateUserId)
            .ToListAsync();
    }

    public async Task<List<CandidateJobLike>> GetLikesForRecruiterOffersAsync(int recruiterUserId)
    {
        var recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(recruiterUserId);
        var recruiterOfferIds = recruiterOffers.Select(x => x.JobOfferId).ToList();

        if (!recruiterOfferIds.Any())
            return new List<CandidateJobLike>();

        var db = await _databaseService.GetConnectionAsync();
        var allLikes = await db.Table<CandidateJobLike>().ToListAsync();

        return allLikes
            .Where(x => recruiterOfferIds.Contains(x.JobOfferId))
            .ToList();
    }

    public async Task<bool> HasCandidateLikedAnyOfferOfRecruiterAsync(int candidateUserId, int recruiterUserId)
    {
        var recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(recruiterUserId);
        var recruiterOfferIds = recruiterOffers.Select(x => x.JobOfferId).ToList();

        if (!recruiterOfferIds.Any())
            return false;

        var db = await _databaseService.GetConnectionAsync();
        var allLikes = await db.Table<CandidateJobLike>().ToListAsync();

        return allLikes.Any(x =>
            x.CandidateUserId == candidateUserId &&
            recruiterOfferIds.Contains(x.JobOfferId));
    }

    public async Task DeleteLikeAsync(int candidateUserId, int jobOfferId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var like = await db.Table<CandidateJobLike>()
            .FirstOrDefaultAsync(x =>
                x.CandidateUserId == candidateUserId &&
                x.JobOfferId == jobOfferId);

        if (like != null)
            await db.DeleteAsync(like);
    }

    public async Task DeleteLikesForCandidateAndRecruiterAsync(int candidateUserId, int recruiterUserId)
    {
        var recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(recruiterUserId);
        var recruiterOfferIds = recruiterOffers.Select(x => x.JobOfferId).ToList();

        if (!recruiterOfferIds.Any())
            return;

        var db = await _databaseService.GetConnectionAsync();
        var likes = await db.Table<CandidateJobLike>().ToListAsync();

        var likesToDelete = likes
            .Where(x => x.CandidateUserId == candidateUserId &&
                        recruiterOfferIds.Contains(x.JobOfferId))
            .ToList();

        foreach (var like in likesToDelete)
        {
            await db.DeleteAsync(like);
        }
    }
}