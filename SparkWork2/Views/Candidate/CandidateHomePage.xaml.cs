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
        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);

        var likedOfferIds = likedOffers
            .Select(x => x.JobOfferId)
            .ToHashSet();

        var availableOffers = allOffers
            .Where(offer => !likedOfferIds.Contains(offer.JobOfferId))
            .ToList();

        var scoredOffers = availableOffers
            .Select(offer => new CandidateHomeOfferItem
            {
                Offer = offer,
                Score = Math.Clamp(_compatibilityService.CalculateScore(_candidateProfile, offer), 0, 100),
                DistanceText = GetShortDistanceDisplay(_candidateProfile, offer),
                DistanceKm = GetDistanceKm(_candidateProfile, offer)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.DistanceKm ?? double.MaxValue)
            .ToList();

        var bestScore = scoredOffers.Any()
            ? scoredOffers.Max(x => x.Score)
            : 0;

        lblCompatibilityValue.Text = bestScore > 0 ? $"{bestScore}%" : "--";
        lblCompatibilitySubtext.Text = scoredOffers.Any()
            ? "meilleure des offres récentes"
            : "aucune offre à analyser";

        int completion = CalculateProfileCompletion(_candidateProfile);

        lblViewedOffersCount.Text = allOffers.Count.ToString();
        lblLikedOffersCount.Text = likedOffers.Count.ToString();
        lblMatchesCount.Text = matches.Count.ToString();
        lblProfileCompletion.Text = $"{completion}%";

        UpdateAdvice(completion);
        UpdateRecommendedOffer(scoredOffers.FirstOrDefault());
        RenderNearbyOffers(scoredOffers.Skip(1).Take(3).ToList());
    }

    private void UpdateRecommendedOffer(CandidateHomeOfferItem? item)
    {
        recommendedTagsLayout.Children.Clear();

        if (item == null)
        {
            _recommendedOffer = null;
            btnRecommendedOffer.IsEnabled = false;

            lblRecommendedLogo.Text = "?";
            lblRecommendedCompanyLocation.Text = "Aucune offre disponible";
            lblRecommendedScore.Text = "--";
            lblRecommendedTitle.Text = "Reviens bientôt";
            lblRecommendedMeta.Text = "De nouvelles opportunités arriveront ici.";
            recommendedCompatibilityProgress.Progress = 0;
            return;
        }

        var offer = item.Offer;
        _recommendedOffer = offer;
        btnRecommendedOffer.IsEnabled = true;

        lblRecommendedLogo.Text = BuildCompanyInitial(offer.CompanyName);
        lblRecommendedCompanyLocation.Text = $"{GetSafeText(offer.CompanyName, "Entreprise")} · {GetSafeText(offer.Location, "Localisation")}";
        lblRecommendedScore.Text = $"{item.Score}%";
        lblRecommendedTitle.Text = GetSafeText(offer.Title, "Offre recommandée");
        lblRecommendedMeta.Text = BuildOfferMeta(offer);
        recommendedCompatibilityProgress.Progress = item.Score / 100.0;

        foreach (var skill in GetOfferSkills(offer).Take(2))
            recommendedTagsLayout.Children.Add(CreateTag(skill, "#F7F4FF", "#CDBDFF", "#4B2FBF"));

        if (offer.HasSalary)
            recommendedTagsLayout.Children.Add(CreateTag(offer.SalaryDisplay, "#F7F4FF", "#CDBDFF", "#4B2FBF"));
    }

    private void RenderNearbyOffers(List<CandidateHomeOfferItem> offers)
    {
        nearbyOffersLayout.Children.Clear();
        lblNoNearbyOffers.IsVisible = offers.Count == 0;

        foreach (var item in offers)
            nearbyOffersLayout.Children.Add(CreateNearbyOfferCard(item));
    }

    private Frame CreateNearbyOfferCard(CandidateHomeOfferItem item)
    {
        var offer = item.Offer;

        var logo = new Frame
        {
            WidthRequest = 48,
            HeightRequest = 48,
            CornerRadius = 14,
            Padding = 0,
            HasShadow = false,
            BackgroundColor = GetLogoColor(item.Score),
            Content = new Label
            {
                Text = BuildCompanyInitial(offer.CompanyName),
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        var title = new Label
        {
            Text = GetSafeText(offer.Title, "Offre"),
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

        var company = new Label
        {
            Text = $"{GetSafeText(offer.CompanyName, "Entreprise")} · {GetSafeText(offer.Location, "Localisation")}",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8581A6"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

        var tags = new HorizontalStackLayout { Spacing = 6 };

        if (!string.IsNullOrWhiteSpace(offer.ContractType))
            tags.Children.Add(CreateTag(offer.ContractType, "#F0EAFE", "#F0EAFE", "#4B2FBF"));

        if (!string.IsNullOrWhiteSpace(offer.Level))
            tags.Children.Add(CreateTag(offer.Level, "#F0EAFE", "#F0EAFE", "#4B2FBF"));

        var infoStack = new VerticalStackLayout
        {
            Spacing = 3,
            Children = { title, company, tags }
        };

        Grid.SetColumn(infoStack, 1);

        var scoreStack = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                new Label
                {
                    Text = $"{item.Score}%",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#7C4DFF"),
                    HorizontalTextAlignment = TextAlignment.End
                },
                new Label
                {
                    Text = item.DistanceText,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#B0ABCE"),
                    HorizontalTextAlignment = TextAlignment.End
                }
            }
        };

        Grid.SetColumn(scoreStack, 2);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 14,
            Children = { logo, infoStack, scoreStack }
        };

        return new Frame
        {
            CornerRadius = 18,
            Padding = 14,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#DED8F5"),
            HasShadow = false,
            Content = grid
        };
    }

    private Frame CreateTag(string text, string background, string border, string textColor)
    {
        return new Frame
        {
            CornerRadius = 12,
            Padding = new Thickness(10, 3),
            BackgroundColor = Color.FromArgb(background),
            BorderColor = Color.FromArgb(border),
            HasShadow = false,
            Margin = new Thickness(0, 0, 6, 6),
            Content = new Label
            {
                Text = text,
                FontSize = 12,
                TextColor = Color.FromArgb(textColor)
            }
        };
    }

    private string GetShortDistanceDisplay(CandidateProfile candidate, JobOffer offer)
    {
        if (!_distanceService.CanCalculateDistance(candidate, offer))
            return offer.Location;

        var distance = _distanceService.CalculateDistanceKm(candidate, offer);
        return distance < 1 ? "< 1 km" : $"{Math.Round(distance)} km";
    }

    private double? GetDistanceKm(CandidateProfile candidate, JobOffer offer)
    {
        if (!_distanceService.CanCalculateDistance(candidate, offer))
            return null;

        return _distanceService.CalculateDistanceKm(candidate, offer);
    }

    private void UpdateAdvice(int completion)
    {
        if (completion >= 90)
        {
            lblAdviceTitle.Text = "Profil solide";
            lblAdviceText.Text = "Votre profil est prêt. Continuez à swiper pour augmenter vos chances de match.";
            return;
        }

        lblAdviceTitle.Text = "Complétez votre profil";
        lblAdviceText.Text = "Ajoutez vos expériences et votre CV pour augmenter votre compatibilité avec les offres recruteurs.";
    }

    private async void RecommendedOffer_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateSwipePage));
    }

    private async void BrowseJobOffers_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateSwipePage));
    }

    private async void CandidateProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateProfilePage));
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

    private static string BuildOfferMeta(JobOffer offer)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(offer.ContractType))
            parts.Add(offer.ContractType);

        if (!string.IsNullOrWhiteSpace(offer.Level))
            parts.Add(offer.Level);

        if (!string.IsNullOrWhiteSpace(offer.RemoteMode))
            parts.Add(offer.RemoteMode);

        return parts.Any()
            ? string.Join(" · ", parts)
            : "Détails à confirmer";
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

    private static string BuildCompanyInitial(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        return value.Trim()[0].ToString().ToUpperInvariant();
    }

    private static string GetSafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static Color GetLogoColor(int score)
    {
        if (score >= 85)
            return Color.FromArgb("#B83EC7");

        if (score >= 70)
            return Color.FromArgb("#17172A");

        return Color.FromArgb("#55B17C");
    }

    private static int CalculateProfileCompletion(CandidateProfile profile)
    {
        int completed = 0;
        int total = 8;

        if (!string.IsNullOrWhiteSpace(profile.FullName)) completed++;
        if (!string.IsNullOrWhiteSpace(profile.Title)) completed++;
        if (!string.IsNullOrWhiteSpace(profile.Location)) completed++;
        if (!string.IsNullOrWhiteSpace(profile.About)) completed++;
        if (!string.IsNullOrWhiteSpace(profile.Skills)) completed++;
        if (!string.IsNullOrWhiteSpace(profile.DesiredContractType)) completed++;
        if (!string.IsNullOrWhiteSpace(profile.ExperienceLevel)) completed++;
        if (profile.DesiredSalaryMin > 0 || profile.DesiredSalaryMax > 0) completed++;

        return (int)Math.Round(completed * 100.0 / total);
    }

    private sealed class CandidateHomeOfferItem
    {
        public JobOffer Offer { get; set; } = new();
        public int Score { get; set; }
        public string DistanceText { get; set; } = string.Empty;
        public double? DistanceKm { get; set; }
    }
}
