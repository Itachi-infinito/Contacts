using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class JobOfferRepository
{
    private readonly DatabaseService _databaseService;

    public JobOfferRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task SeedDataAsync()
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingOffers = await db.Table<JobOffer>().ToListAsync();
        if (existingOffers.Any())
            return;

        var seedOffers = new List<JobOffer>
        {
            new JobOffer
            {
                RecruiterId = 0,
                Title = "Frontend Developer",
                CompanyName = "TechCorp",
                Location = "Brussels",
                ContractType = "Full-time",
                Description = "Develop and maintain modern user interfaces."
            },
            new JobOffer
            {
                RecruiterId = 0,
                Title = "Marketing Intern",
                CompanyName = "Startup Vision",
                Location = "Liège",
                ContractType = "Internship",
                Description = "Support the marketing team in digital campaigns."
            },
            new JobOffer
            {
                RecruiterId = 0,
                Title = "Backend Developer",
                CompanyName = "DigitalWorks",
                Location = "Namur",
                ContractType = "Full-time",
                Description = "Design APIs and manage server-side logic."
            }
        };

        await db.InsertAllAsync(seedOffers);
    }

    public async Task<List<JobOffer>> GetJobOffersAsync()
    {
        var db = await _databaseService.GetConnectionAsync();
        return await db.Table<JobOffer>().ToListAsync();
    }

    public async Task<List<JobOffer>> GetJobOffersByRecruiterAsync(int recruiterUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<JobOffer>()
            .Where(x => x.RecruiterId == recruiterUserId)
            .ToListAsync();
    }

    public async Task<JobOffer?> GetJobOfferByIdAsync(int jobOfferId)
    {
        var db = await _databaseService.GetConnectionAsync();

        return await db.Table<JobOffer>()
            .FirstOrDefaultAsync(x => x.JobOfferId == jobOfferId);
    }

    public async Task AddJobOfferAsync(JobOffer jobOffer)
    {
        var db = await _databaseService.GetConnectionAsync();
        await db.InsertAsync(jobOffer);
    }

    public async Task UpdateJobOfferAsync(int jobOfferId, JobOffer jobOffer)
    {
        var db = await _databaseService.GetConnectionAsync();

        if (jobOfferId != jobOffer.JobOfferId)
            return;

        await db.UpdateAsync(jobOffer);
    }

    public async Task DeleteJobOfferAsync(int jobOfferId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var jobOffer = await GetJobOfferByIdAsync(jobOfferId);
        if (jobOffer != null)
            await db.DeleteAsync(jobOffer);
    }

    public async Task<List<JobOffer>> SearchJobOffersAsync(string filterText)
    {
        var db = await _databaseService.GetConnectionAsync();

        if (string.IsNullOrWhiteSpace(filterText))
            return await GetJobOffersAsync();

        filterText = filterText.ToLower();

        var allOffers = await db.Table<JobOffer>().ToListAsync();

        return allOffers.Where(x =>
            (!string.IsNullOrWhiteSpace(x.Title) && x.Title.ToLower().Contains(filterText)) ||
            (!string.IsNullOrWhiteSpace(x.CompanyName) && x.CompanyName.ToLower().Contains(filterText)) ||
            (!string.IsNullOrWhiteSpace(x.Location) && x.Location.ToLower().Contains(filterText))
        ).ToList();
    }
}