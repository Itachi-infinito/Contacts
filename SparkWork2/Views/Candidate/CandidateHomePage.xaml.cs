using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Candidate;

public partial class CandidateHomePage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly CompatibilityService _compatibilityService;
    private readonly DistanceService _distanceService;

    private CandidateProfile? _candidateProfile;
    private JobOffer? _recommendedOffer;
    private bool _isCheckingPendingMatch;

    public CandidateHomePage(
        MatchRepository matchRepository,
        SessionService sessionService,
        CandidateProfileRepository candidateProfileRepository,
        JobOfferRepository jobOfferRepository,
        CandidateJobLikeRepository candidateJobLikeRepository,
        CompatibilityService compatibilityService,
        DistanceService distanceService)
    {
        InitializeComponent();

        _matchRepository = matchRepository;
        _sessionService = sessionService;
        _candidateProfileRepository = candidateProfileRepository;
        _jobOfferRepository = jobOfferRepository;
        _candidateJobLikeRepository = candidateJobLikeRepository;
        _compatibilityService = compatibilityService;
        _distanceService = distanceService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        lblGreeting.Text = $"Bonjour, {_sessionService.CurrentUserName}";
        lblInitials.Text = BuildInitials(_sessionService.CurrentUserName);

        await LoadDashboardAsync();

        if (_isCheckingPendingMatch || !_sessionService.IsLoggedIn)
            return;

        _isCheckingPendingMatch = true;

        try
        {
            var pendingMatch = await _matchRepository.GetPendingMatchForCandidateAsync(_sessionService.CurrentUserId);

            if (pendingMatch != null)
            {
                await _matchRepository.MarkShownToCandidateAsync(pendingMatch.MatchId);

                await Shell.Current.GoToAsync(
                    $"{nameof(MatchPage)}" +
                    $"?participantId={pendingMatch.RecruiterUserId}" +
                    $"&participantName={Uri.EscapeDataString(pendingMatch.CompanyName)}");
            }
        }
        finally
        {
            _isCheckingPendingMatch = false;
        }
    }

    private async Task LoadDashboardAsync()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        _candidateProfile = await _candidateProfileRepository.GetCandidateProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        lblDistanceValue.Text = _candidateProfile.MaxDistanceKm > 0
            ? $"{_candidateProfile.MaxDistanceKm} km"
            : "--";

        lblDistanceSubtext.Text = _candidateProfile.MaxDistanceKm > 0
            ? "préférence active"
            : "à configurer";

        var allOffers = await _jobOfferRepository.GetJobOffersAsync();
        var likedOffers = await _candidateJobLikeRepository.GetLikesByCandidateAsync(_sessionService.CurrentUserId);

        var likedOfferIds = likedOffers
            .Select(x => x.JobOfferId)
            .ToHashSet();

        var availableOffers = allOffers
            .Where(offer => !likedOfferIds.Contains(offer.JobOfferId))
            .ToList();

        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);

        lblViewedOffersCount.Text = availableOffers.Count.ToString();
        lblLikedOffersCount.Text = likedOffers.Count.ToString();
        lblMatchesCount.Text = matches.Count.ToString();
        lblProfileCompletion.Text = $"{CalculateProfileCompletion(_candidateProfile)}%";


        var scoredOffers = availableOffers
            .Select(offer => new
            {
                Offer = offer,
                Score = _compatibilityService.CalculateScore(_candidateProfile, offer)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var recentScores = scoredOffers.Take(10).ToList();

        lblCompatibilityValue.Text = recentScores.Any()
            ? $"{Math.Round(recentScores.Average(x => x.Score))}%"
            : "--";

        lblCompatibilitySubtext.Text = recentScores.Any()
            ? "moyenne des offres récentes"
            : "aucune offre à analyser";

        var recommendation = scoredOffers.FirstOrDefault();
        UpdateRecommendedOffer(recommendation?.Offer, recommendation?.Score);
    }

    private void UpdateRecommendedOffer(JobOffer? offer, int? score)
    {
        _recommendedOffer = offer;
        recommendedSkillsLayout.Children.Clear();

        if (offer == null)
        {
            lblRecommendedTitle.Text = "Aucune offre disponible";
            lblRecommendedScore.Text = "--";
            lblRecommendedMeta.Text = "Reviens plus tard pour de nouvelles offres.";
            return;
        }

        lblRecommendedTitle.Text = offer.Title;
        lblRecommendedScore.Text = $"{score ?? 0}%";

        var meta = new List<string>();

        if (!string.IsNullOrWhiteSpace(offer.Location))
            meta.Add(offer.Location);

        if (!string.IsNullOrWhiteSpace(offer.ContractType))
            meta.Add(offer.ContractType);

        if (_candidateProfile != null)
        {
            string distance = _distanceService.GetDistanceDisplay(_candidateProfile, offer);
            if (!string.IsNullOrWhiteSpace(distance))
                meta.Add(distance);
        }

        lblRecommendedMeta.Text = meta.Any()
            ? string.Join(" · ", meta)
            : "Offre recommandée selon ton profil";

        foreach (var skill in GetOfferSkills(offer).Take(3))
        {
            recommendedSkillsLayout.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#F0EAFE"),
                CornerRadius = 12,
                Padding = new Thickness(9, 3),
                Margin = new Thickness(0, 0, 8, 0),
                HasShadow = false,
                Content = new Label
                {
                    Text = skill,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#7C4DFF")
                }
            });
        }
    }

    private async void BrowseJobOffers_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateSwipePage));
    }

    private async void Matches_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MessagesPage));
    }


    private async void CandidateProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateProfilePage));
    }

    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void Messages_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MessagesPage));
    }

    private static List<string> GetOfferSkills(JobOffer offer)
    {
        var source = !string.IsNullOrWhiteSpace(offer.RequiredSkills)
            ? offer.RequiredSkills
            : offer.NiceToHaveSkills;

        if (string.IsNullOrWhiteSpace(source))
            return new List<string>();

        return source
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "ME";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static int CalculateProfileCompletion(CandidateProfile profile)
    {
        int completed = 0;
        int total = 8;

        if (!string.IsNullOrWhiteSpace(profile.FullName))
            completed++;

        if (!string.IsNullOrWhiteSpace(profile.Title))
            completed++;

        if (!string.IsNullOrWhiteSpace(profile.Location))
            completed++;

        if (!string.IsNullOrWhiteSpace(profile.About))
            completed++;

        if (!string.IsNullOrWhiteSpace(profile.Skills))
            completed++;

        if (!string.IsNullOrWhiteSpace(profile.DesiredContractType))
            completed++;

        if (!string.IsNullOrWhiteSpace(profile.ExperienceLevel))
            completed++;

        if (profile.DesiredSalaryMin > 0 || profile.DesiredSalaryMax > 0)
            completed++;

        return (int)Math.Round(completed * 100.0 / total);
    }

}
