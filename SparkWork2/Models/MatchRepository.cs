using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class MatchRepository
{
    private readonly DatabaseService _databaseService;

    public MatchRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<Match>> GetMatchesAsync(int userId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<Match>()
            .Where(x => x.CandidateUserId == userId || x.RecruiterUserId == userId)
            .ToListAsync();
    }

    public async Task<bool> AddMatchAsync(
        int candidateUserId,
        string candidateName,
        int recruiterUserId,
        JobOffer jobOffer,
        bool createdByCandidate)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingMatch = await db.Table<Match>()
            .FirstOrDefaultAsync(x =>
                x.CandidateUserId == candidateUserId &&
                x.RecruiterUserId == recruiterUserId &&
                x.JobOfferId == jobOffer.JobOfferId);

        if (existingMatch != null)
            return false;

        var match = new Match
        {
            UserId = candidateUserId,
            CandidateUserId = candidateUserId,
            CandidateName = candidateName,
            RecruiterUserId = recruiterUserId,
            JobOfferId = jobOffer.JobOfferId,
            JobTitle = jobOffer.Title,
            CompanyName = jobOffer.CompanyName,

            // Celui qui crée le match voit déjà l'animation immédiatement.
            // L'autre devra la voir à sa prochaine connexion.
            ShowToCandidate = !createdByCandidate,
            ShowToRecruiter = createdByCandidate
        };

        await db.InsertAsync(match);
        return true;
    }

    public async Task RemoveMatchAsync(int matchId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var match = await db.Table<Match>()
            .FirstOrDefaultAsync(x => x.MatchId == matchId);

        if (match != null)
            await db.DeleteAsync(match);
    }

    public async Task<bool> IsMatchExistsAsync(int jobOfferId, int userId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingMatch = await db.Table<Match>()
            .FirstOrDefaultAsync(x =>
                x.JobOfferId == jobOfferId &&
                (x.CandidateUserId == userId || x.RecruiterUserId == userId));

        return existingMatch != null;
    }

    public async Task<List<RecruiterMatchItem>> GetMatchesForRecruiterAsync(int recruiterUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var matches = await db.Table<Match>()
            .Where(x => x.RecruiterUserId == recruiterUserId)
            .ToListAsync();

        var result = new List<RecruiterMatchItem>();

        foreach (var match in matches)
        {
            var user = await db.Table<User>()
                .FirstOrDefaultAsync(x => x.UserId == match.CandidateUserId);

            result.Add(new RecruiterMatchItem
            {
                MatchId = match.MatchId,
                CandidateUserId = match.CandidateUserId,
                CandidateName = user?.FullName ?? match.CandidateName,
                JobOfferId = match.JobOfferId,
                JobTitle = match.JobTitle
            });
        }

        return result;
    }

    public async Task<List<RecruiterLikeReceivedItem>> GetLikesReceivedByRecruiterAsync(int recruiterUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var matches = await db.Table<Match>()
            .Where(x => x.RecruiterUserId == recruiterUserId)
            .ToListAsync();

        var result = new List<RecruiterLikeReceivedItem>();

        foreach (var match in matches)
        {
            var user = await db.Table<User>()
                .FirstOrDefaultAsync(x => x.UserId == match.CandidateUserId);

            if (user == null)
                continue;

            result.Add(new RecruiterLikeReceivedItem
            {
                CandidateUserId = match.CandidateUserId,
                CandidateName = user.FullName,
                JobOfferId = match.JobOfferId,
                JobTitle = match.JobTitle
            });
        }

        return result;
    }

    public async Task<bool> HasCandidateLikedAnyOfferOfRecruiterAsync(int candidateUserId, int recruiterUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var recruiterOffers = await db.Table<JobOffer>()
            .Where(x => x.RecruiterUserId == recruiterUserId)
            .ToListAsync();

        var recruiterOfferIds = recruiterOffers
            .Select(x => x.JobOfferId)
            .ToList();

        if (!recruiterOfferIds.Any())
            return false;

        var candidateLikes = await db.Table<CandidateJobLike>()
            .Where(x => x.CandidateUserId == candidateUserId)
            .ToListAsync();

        return candidateLikes.Any(x => recruiterOfferIds.Contains(x.JobOfferId));
    }

    public async Task<Match?> GetPendingMatchForCandidateAsync(int candidateUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<Match>()
            .Where(x => x.CandidateUserId == candidateUserId && x.ShowToCandidate)
            .OrderByDescending(x => x.MatchId)
            .FirstOrDefaultAsync();
    }

    public async Task<Match?> GetPendingMatchForRecruiterAsync(int recruiterUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<Match>()
            .Where(x => x.RecruiterUserId == recruiterUserId && x.ShowToRecruiter)
            .OrderByDescending(x => x.MatchId)
            .FirstOrDefaultAsync();
    }

    public async Task<Match?> GetPendingMatchAnimationAsync(int userId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var matches = await db.Table<Match>()
            .Where(x =>
                (x.CandidateUserId == userId && x.ShowToCandidate) ||
                (x.RecruiterUserId == userId && x.ShowToRecruiter))
            .ToListAsync();

        return matches
            .OrderByDescending(x => x.MatchId)
            .FirstOrDefault();
    }

    public async Task MarkShownToCandidateAsync(int matchId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var match = await db.Table<Match>()
            .FirstOrDefaultAsync(x => x.MatchId == matchId);

        if (match == null)
            return;

        match.ShowToCandidate = false;
        await db.UpdateAsync(match);
    }

    public async Task MarkShownToRecruiterAsync(int matchId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var match = await db.Table<Match>()
            .FirstOrDefaultAsync(x => x.MatchId == matchId);

        if (match == null)
            return;

        match.ShowToRecruiter = false;
        await db.UpdateAsync(match);
    }

    public async Task MarkMatchAnimationSeenAsync(int matchId, int userId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var match = await db.Table<Match>()
            .FirstOrDefaultAsync(x => x.MatchId == matchId);

        if (match == null)
            return;

        if (match.CandidateUserId == userId)
            match.ShowToCandidate = false;

        if (match.RecruiterUserId == userId)
            match.ShowToRecruiter = false;

        await db.UpdateAsync(match);
    }
}
