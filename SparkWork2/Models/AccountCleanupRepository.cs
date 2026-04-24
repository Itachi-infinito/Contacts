using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class AccountCleanupRepository
{
    private readonly DatabaseService _databaseService;

    public AccountCleanupRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task DeleteAccountCompletelyAsync(int userId, string role)
    {
        if (userId <= 0)
            return;

        var db = await _databaseService.GetConnectionAsync();

        // 1. Supprimer les messages liés
        var allMessages = await db.Table<Message>().ToListAsync();
        var messagesToDelete = allMessages
            .Where(x => x.SenderUserId == userId || x.ReceiverUserId == userId)
            .ToList();

        foreach (var message in messagesToDelete)
        {
            await db.DeleteAsync(message);
        }

        // 2. Supprimer les matchs liés
        var allMatches = await db.Table<Match>().ToListAsync();
        var matchesToDelete = allMatches
            .Where(x =>
                x.UserId == userId ||
                x.CandidateUserId == userId ||
                x.RecruiterUserId == userId)
            .ToList();

        foreach (var match in matchesToDelete)
        {
            await db.DeleteAsync(match);
        }

        // 3. Supprimer les likes candidat
        var candidateLikes = await db.Table<CandidateJobLike>()
            .Where(x => x.CandidateUserId == userId)
            .ToListAsync();

        foreach (var like in candidateLikes)
        {
            await db.DeleteAsync(like);
        }

        // 4. Supprimer les likes recruteur
        var recruiterLikes = await db.Table<RecruiterCandidateLike>()
            .Where(x => x.CandidateUserId == userId || x.RecruiterUserId == userId)
            .ToListAsync();

        foreach (var like in recruiterLikes)
        {
            await db.DeleteAsync(like);
        }

        // 5. Supprimer le profil candidat
        var candidateProfile = await db.Table<CandidateProfile>()
            .FirstOrDefaultAsync(x => x.CandidateId == userId);

        if (candidateProfile != null)
            await db.DeleteAsync(candidateProfile);

        // 6. Supprimer le profil recruteur
        var recruiterProfile = await db.Table<RecruiterProfile>()
            .FirstOrDefaultAsync(x => x.RecruiterId == userId);

        if (recruiterProfile != null)
            await db.DeleteAsync(recruiterProfile);

        // 7. Supprimer les offres si recruteur
        var offersToDelete = await db.Table<JobOffer>()
            .Where(x => x.RecruiterUserId == userId)
            .ToListAsync();

        foreach (var offer in offersToDelete)
        {
            await db.DeleteAsync(offer);
        }

        // 8. Supprimer l'utilisateur
        var user = await db.Table<User>()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (user != null)
            await db.DeleteAsync(user);
    }
}