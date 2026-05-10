using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;
using System.Globalization;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterHomePage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly CompatibilityService _compatibilityService;

    private bool _isCheckingPendingMatch;
    private const double ProgressBarMaxWidth = 260;

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

        if (!_sessionService.IsLoggedIn) return;

        lblGreeting.Text = $"Bonjour, {_sessionService.CurrentUserName}";
        lblInitials.Text = BuildInitials(_sessionService.CurrentUserName);

        await LoadDashboardData();

        if (_isCheckingPendingMatch) return;
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

    private async Task LoadDashboardData()
    {
        if (!_sessionService.IsLoggedIn) return;

        var candidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        candidates = candidates
            .Where(c => c.CandidateId != _sessionService.CurrentUserId)
            .ToList();

        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);

        lblCandidatesCount.Text = candidates.Count.ToString();

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
            lblActiveOfferInitial.Text = "?";
            lblActiveOfferTitle.Text = "Aucune offre active";
            lblActiveOfferInfo.Text = "Publie une offre pour commencer";
            lblActiveOfferCandidates.Text = "0 profil";
            return;
        }

        lblActiveOfferInitial.Text = firstOffer.Title.Length > 0
            ? firstOffer.Title[0].ToString().ToUpperInvariant()
            : "?";
        lblActiveOfferTitle.Text = firstOffer.Title;
        lblActiveOfferInfo.Text = $"{firstOffer.Location} · {firstOffer.ContractType}";

        int compatibleCount = candidates.Count(c =>
            _compatibilityService.CalculateScore(c, firstOffer) >= 50);

        lblActiveOfferCandidates.Text = compatibleCount <= 1
            ? $"{compatibleCount} profil"
            : $"{compatibleCount} profils";
    }

    private void UpdateRecommendedCandidateSection(List<CandidateProfile> candidates, List<JobOffer> offers)
    {
        recommendedSkillsLayout.Children.Clear();
        recommendedProgressContainer.IsVisible = false;
        recommendedProgressBar.WidthRequest = 0;
        viewProfileFrame.IsVisible = false;

        if (!candidates.Any() || !offers.Any())
        {
            lblRecommendedCandidateName.Text = "Aucun candidat";
            lblRecommendedCandidateScore.Text = "";
            lblRecommendedCandidateInfo.Text = "Crée une offre pour obtenir une recommandation";
            return;
        }

        var bestMatch = candidates
            .SelectMany(c => offers.Select(o => new
            {
                Candidate = c,
                Offer = o,
                Score = _compatibilityService.CalculateScore(c, o)
            }))
            .OrderByDescending(x => x.Score)
            .First();

        // Capitalize name
        var rawName = bestMatch.Candidate.FullName;
        lblRecommendedCandidateName.Text = string.IsNullOrWhiteSpace(rawName)
            ? "Candidat"
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName.ToLower());

        lblRecommendedCandidateScore.Text = $"{bestMatch.Score}%";
        lblRecommendedCandidateScore.TextColor = bestMatch.Score >= 75
            ? Color.FromArgb("#10B981")
            : bestMatch.Score >= 45
                ? Color.FromArgb("#7C4DFF")
                : Color.FromArgb("#E11D48");

        // Info line: Location · CandidateTitle · OfferTitle
        var infoParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(bestMatch.Candidate.Location))
            infoParts.Add(bestMatch.Candidate.Location);
        if (!string.IsNullOrWhiteSpace(bestMatch.Candidate.Title))
            infoParts.Add(bestMatch.Candidate.Title.ToLower());
        if (!string.IsNullOrWhiteSpace(bestMatch.Offer.Title))
            infoParts.Add(bestMatch.Offer.Title);

        lblRecommendedCandidateInfo.Text = infoParts.Any()
            ? string.Join(" · ", infoParts)
            : "Profil candidat";

        // Skill chips
        var skills = SplitValues(bestMatch.Candidate.Skills).Take(2).ToList();
        foreach (var skill in skills)
        {
            recommendedSkillsLayout.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#F0EAFE"),
                BorderColor = Color.FromArgb("#CFC4FF"),
                CornerRadius = 10,
                Padding = new Thickness(12, 4),
                HasShadow = false,
                Margin = new Thickness(0, 0, 8, 6),
                Content = new Label
                {
                    Text = skill,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#7C4DFF")
                }
            });
        }

        // Progress bar
        if (bestMatch.Score > 0)
        {
            recommendedProgressContainer.IsVisible = true;
            recommendedProgressBar.WidthRequest = Math.Max(4, ProgressBarMaxWidth * bestMatch.Score / 100.0);
        }

        viewProfileFrame.IsVisible = true;
    }

    // ── Navigation ────────────────────────────────────────────────────────

    private async void ViewRecommendedProfile_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));

    private async void BrowseCandidates_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));

    private async void BrowseCandidates_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));

    private async void JobOffers_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterJobOffersPage));

    private async void JobOffers_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterJobOffersPage));

    private async void Matches_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterMatchesPage));

    private async void Matches_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterMatchesPage));

    private async void Messages_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(MessagesPage));

    private async void AddJobOffer_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));

    private async void RecruiterProfile_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterProfilePage));

    private async void Discover_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));

    private async void AddOffer_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));

    private async void Messages_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(MessagesPage));

    private async void Profile_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");

    private async void Settings_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(SettingsPage));

    private async void LikesReceived_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(LikesReceivedPage));

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "AL";
        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][0].ToString().ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static List<string> SplitValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new();
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
