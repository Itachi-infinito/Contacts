using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterHomePage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly CompatibilityService _compatibilityService;


    private bool _isCheckingPendingMatch;

    public RecruiterHomePage(
    MatchRepository matchRepository,
    SessionService sessionService,
    CandidateProfileRepository candidateProfileRepository,
    JobOfferRepository jobOfferRepository,
    CompatibilityService compatibilityService)

    {
        InitializeComponent();
        _matchRepository = matchRepository;
        _sessionService = sessionService;
        _candidateProfileRepository = candidateProfileRepository;
        _jobOfferRepository = jobOfferRepository;
        _compatibilityService = compatibilityService;

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_sessionService.IsLoggedIn)
            return;

        lblGreeting.Text = $"Bonjour, {_sessionService.CurrentUserName}";
        lblInitials.Text = BuildInitials(_sessionService.CurrentUserName);

        await LoadDashboardData();

        if (_isCheckingPendingMatch)
            return;

        _isCheckingPendingMatch = true;

        try
        {
            var pendingMatch = await _matchRepository.GetPendingMatchForRecruiterAsync(_sessionService.CurrentUserId);

            if (pendingMatch != null)
            {
                await _matchRepository.MarkShownToRecruiterAsync(pendingMatch.MatchId);

                await Shell.Current.GoToAsync(
                    $"{nameof(MatchPage)}" +
                    $"?participantId={pendingMatch.CandidateUserId}" +
                    $"&participantName={Uri.EscapeDataString(pendingMatch.CandidateName)}");
            }
        }
        finally
        {
            _isCheckingPendingMatch = false;
        }
    }


    private async void BrowseCandidates_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }


    private async void JobOffers_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterJobOffersPage));
    }

    private async void Matches_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterMatchesPage));
    }

    private async void Messages_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MessagesPage));
    }

    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void AddJobOffer_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));
    }

    private async void RecruiterProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterProfilePage));
    }

    private async void LikesReceived_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LikesReceivedPage));
    }
    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "AL";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
    private async Task LoadDashboardData()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var candidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();

        candidates = candidates
            .Where(candidate => candidate.CandidateId != _sessionService.CurrentUserId)
            .ToList();

        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(
            _sessionService.CurrentUserId);

        lblCandidatesCount.Text = candidates.Count.ToString();

        // Si ton MatchRepository a une méthode GetMatchesForUserAsync ou GetMatchesAsync,
        // utilise celle que tu as réellement.
        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);
        lblMatchesCount.Text = matches.Count.ToString();

        UpdateActiveOfferSection(offers, candidates);
        UpdateRecommendedCandidateSection(candidates, offers);
    }
    private void UpdateActiveOfferSection(List<JobOffer> offers, List<CandidateProfile> candidates)
    {
        var firstOffer = offers.FirstOrDefault();

        if (firstOffer == null)
        {
            lblActiveOfferTitle.Text = "Aucune offre active";
            lblActiveOfferInfo.Text = "Publie une offre pour commencer";
            lblActiveOfferCandidates.Text = "0 profil";
            return;
        }

        lblActiveOfferTitle.Text = firstOffer.Title;
        lblActiveOfferInfo.Text = $"{firstOffer.Location} · {firstOffer.ContractType}";

        int compatibleCount = candidates.Count(candidate =>
            _compatibilityService.CalculateScore(candidate, firstOffer) >= 50);

        lblActiveOfferCandidates.Text = compatibleCount <= 1
            ? $"{compatibleCount} profil"
            : $"{compatibleCount} profils";
    }

    private void UpdateRecommendedCandidateSection(List<CandidateProfile> candidates, List<JobOffer> offers)
    {
        if (!candidates.Any() || !offers.Any())
        {
            lblRecommendedCandidateName.Text = "Aucun candidat";
            lblRecommendedCandidateScore.Text = "0%";
            lblRecommendedCandidateInfo.Text = "Crée une offre pour obtenir une recommandation";
            return;
        }

        var bestMatch = candidates
            .SelectMany(candidate => offers.Select(offer => new
            {
                Candidate = candidate,
                Offer = offer,
                Score = _compatibilityService.CalculateScore(candidate, offer)
            }))
            .OrderByDescending(x => x.Score)
            .First();

        lblRecommendedCandidateName.Text = string.IsNullOrWhiteSpace(bestMatch.Candidate.FullName)
            ? "Candidat"
            : bestMatch.Candidate.FullName;

        lblRecommendedCandidateScore.Text = $"{bestMatch.Score}%";

        var location = string.IsNullOrWhiteSpace(bestMatch.Candidate.Location)
            ? "Localisation non renseignée"
            : bestMatch.Candidate.Location;

        var contract = string.IsNullOrWhiteSpace(bestMatch.Candidate.DesiredContractType)
            ? "Contrat non renseigné"
            : bestMatch.Candidate.DesiredContractType;

        lblRecommendedCandidateInfo.Text =
            $"{location} · {contract} · pour {bestMatch.Offer.Title}";
    }

}