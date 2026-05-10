using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;
using System.Globalization;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterSwipePage : ContentPage
{
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly MatchRepository _matchRepository;
    private readonly RecruiterCandidateLikeRepository _recruiterCandidateLikeRepository;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly SessionService _sessionService;
    private readonly DistanceService _distanceService;
    private readonly CompatibilityService _compatibilityService;

    private List<JobOffer> _recruiterOffers = new();
    private JobOffer? _selectedJobOffer;
    private bool _isLoadingOfferFilter;

    private List<CandidateProfile> _candidates = new();
    private int _currentIndex;
    private CandidateProfile? _currentCandidate;
    private string _currentLocationBase = string.Empty;

    private double _panX;
    private bool _isExpanded;
    private bool _isAnimating;

    private const double SwipeThreshold = 120;
    private const double ProgressBarMaxWidth = 118;

    public RecruiterSwipePage(
        CandidateJobLikeRepository candidateJobLikeRepository,
        JobOfferRepository jobOfferRepository,
        MatchRepository matchRepository,
        RecruiterCandidateLikeRepository recruiterCandidateLikeRepository,
        CandidateProfileRepository candidateProfileRepository,
        DistanceService distanceService,
        CompatibilityService compatibilityService,
        SessionService sessionService)
    {
        InitializeComponent();
        _candidateJobLikeRepository = candidateJobLikeRepository;
        _jobOfferRepository = jobOfferRepository;
        _matchRepository = matchRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _candidateProfileRepository = candidateProfileRepository;
        _sessionService = sessionService;
        _distanceService = distanceService;
        _compatibilityService = compatibilityService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            lblHeaderInitials.Text = BuildInitials(_sessionService.CurrentUserName);
            await LoadRecruiterOffers();
            await LoadCandidates();
            await ShowPendingMatchAnimationIfNeeded();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RecruiterSwipePage error: {ex}");
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }

    private async Task LoadRecruiterOffers()
    {
        if (!_sessionService.IsLoggedIn) return;

        _isLoadingOfferFilter = true;
        _recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(
            _sessionService.CurrentUserId);

        offerFilterPicker.ItemsSource = null;
        offerFilterPicker.ItemsSource = _recruiterOffers;
        offerFilterCard.IsVisible = true;
        _isLoadingOfferFilter = false;
    }

    private async Task LoadCandidates()
    {
        if (!_sessionService.IsLoggedIn)
        {
            ShowEmptyState("Connecte-toi pour voir les candidats.");
            return;
        }

        var allCandidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        var recruiterLikes = await _recruiterCandidateLikeRepository.GetLikesByRecruiterAsync(
            _sessionService.CurrentUserId);

        _candidates = allCandidates
            .Where(c => c.CandidateId != _sessionService.CurrentUserId)
            .Where(c => !recruiterLikes.Any(l => l.CandidateUserId == c.CandidateId))
            .ToList();

        if (_selectedJobOffer != null)
        {
            _candidates = _candidates
                .OrderByDescending(c => _compatibilityService.CalculateScore(c, _selectedJobOffer))
                .ToList();
        }

        _currentIndex = 0;
        ShowCurrentCandidate();
    }

    private void ShowCurrentCandidate()
    {
        ResetCardVisuals();
        ResetExpandedState();

        if (_candidates.Count == 0 || _currentIndex >= _candidates.Count)
        {
            ShowEmptyState("Revenez bientôt pour découvrir de nouveaux candidats.");
            return;
        }

        _currentCandidate = _candidates[_currentIndex];

        candidateCard.IsVisible = true;
        emptyStateSection.IsVisible = false;
        cardScrollView.IsVisible = true;
        lblSwipeHint.IsVisible = true;
        actionsGrid.IsVisible = true;

        // Capitalize name properly : "adel chfik" → "Adel Chfik"
        var rawName = _currentCandidate.FullName;
        var fullName = string.IsNullOrWhiteSpace(rawName)
            ? "Candidat"
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName.ToLower());

        var title = string.IsNullOrWhiteSpace(_currentCandidate.Title)
            ? "Profil candidat"
            : _currentCandidate.Title;

        _currentLocationBase = string.IsNullOrWhiteSpace(_currentCandidate.Location)
            ? "Localisation non renseignée"
            : _currentCandidate.Location;

        var about = string.IsNullOrWhiteSpace(_currentCandidate.About)
            ? "Profil candidat disponible pour une nouvelle opportunité."
            : _currentCandidate.About;

        lblFullName.Text = fullName;
        lblTitle.Text = title;
        lblLocation.Text = _currentLocationBase;   // distance appended async below
        lblAbout.Text = about;
        aboutSection.IsVisible = true;

        UpdateCandidateBadges(_currentCandidate);
        UpdateCandidatePreferences(_currentCandidate);
        UpdateCandidateDetailFields(_currentCandidate);
        UpdateExperienceSection(_currentCandidate);
        SetCandidateImage(_currentCandidate);

        _ = UpdateBestCompatibility(_currentCandidate);
        _ = UpdateDistanceAndLocation(_currentCandidate);
    }

    private void ShowEmptyState(string message)
    {
        _currentCandidate = null;
        _panX = 0;
        _isAnimating = false;
        _isExpanded = false;

        candidateCard.IsVisible = false;
        emptyStateSection.IsVisible = true;
        lblEmptyMessage.Text = message;
        expandedDetailsSection.IsVisible = false;
        expandedDetailsSection.Opacity = 0;
        lblLikeOverlay.Opacity = 0;
        lblNopeOverlay.Opacity = 0;
        lblSwipeHint.IsVisible = false;
        actionsGrid.IsVisible = false;
    }

    private void ResetCardVisuals()
    {
        _panX = 0;
        _isAnimating = false;
        candidateCard.TranslationX = 0;
        candidateCard.TranslationY = 0;
        candidateCard.Rotation = 0;
        candidateCard.Opacity = 1;
        lblLikeOverlay.Opacity = 0;
        lblNopeOverlay.Opacity = 0;
    }

    private void ResetExpandedState()
    {
        _isExpanded = false;
        expandedDetailsSection.IsVisible = false;
        expandedDetailsSection.Opacity = 0;
    }

    // ── Swipe ────────────────────────────────────────────────────────────

    private void Card_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (_currentCandidate == null || _isAnimating) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                _panX = e.TotalX;
                candidateCard.TranslationX = e.TotalX;
                candidateCard.Rotation = e.TotalX / 20;
                lblLikeOverlay.Opacity = e.TotalX > 0 ? Math.Min(e.TotalX / 120.0, 1) : 0;
                lblNopeOverlay.Opacity = e.TotalX < 0 ? Math.Min(-e.TotalX / 120.0, 1) : 0;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _ = HandleSwipeCompletedAsync();
                break;
        }
    }

    private async Task HandleSwipeCompletedAsync()
    {
        if (_isAnimating) return;

        if (_panX >= SwipeThreshold)
        {
            await AnimateCardOutAsync(true);
            await PerformLikeAsync();
        }
        else if (_panX <= -SwipeThreshold)
        {
            await AnimateCardOutAsync(false);
            PerformReject();
        }
        else
        {
            await Task.WhenAll(
                candidateCard.TranslateTo(0, 0, 180, Easing.SpringOut),
                candidateCard.RotateTo(0, 180, Easing.SpringOut),
                lblLikeOverlay.FadeTo(0, 120),
                lblNopeOverlay.FadeTo(0, 120));
            _panX = 0;
        }
    }

    private async Task AnimateCardOutAsync(bool toRight)
    {
        _isAnimating = true;
        var targetX = toRight ? 600 : -600;
        await Task.WhenAll(
            candidateCard.TranslateTo(targetX, 0, 240, Easing.CubicIn),
            candidateCard.RotateTo(toRight ? 20 : -20, 240, Easing.CubicIn),
            candidateCard.FadeTo(0, 220));
    }

    // ── Like / Reject / SuperLike ────────────────────────────────────────

    private async Task PerformLikeAsync(bool isSuperLike = false)

    {
        if (_currentCandidate == null || !_sessionService.IsLoggedIn)
        {
            _isAnimating = false;
            return;
        }

        var liked = _currentCandidate;



        await _recruiterCandidateLikeRepository.AddLikeAsync(
            _sessionService.CurrentUserId,
            liked.CandidateId,
            isSuperLike);


        var matchedOffer = await _candidateJobLikeRepository.GetFirstLikedOfferOfRecruiterAsync(
            liked.CandidateId,
            _sessionService.CurrentUserId);

        _candidates.RemoveAt(_currentIndex);

        if (matchedOffer != null)
        {
            await _matchRepository.AddMatchAsync(
                liked.CandidateId,
                liked.FullName,
                _sessionService.CurrentUserId,
                matchedOffer,
                false);

            await Shell.Current.GoToAsync(
                $"{nameof(MatchPage)}" +
                $"?participantId={liked.CandidateId}" +
                $"&participantName={Uri.EscapeDataString(liked.FullName)}");
            return;
        }

        ShowCurrentCandidate();
    }

    private void PerformReject()
    {
        if (_currentCandidate == null) { _isAnimating = false; return; }
        _candidates.RemoveAt(_currentIndex);
        ShowCurrentCandidate();
    }

    private async void Like_Clicked(object sender, EventArgs e)
    {
        if (_currentCandidate == null || _isAnimating) return;
        lblLikeOverlay.Opacity = 1;
        await AnimateCardOutAsync(true);
        await PerformLikeAsync();
    }

    private async void Reject_Clicked(object sender, EventArgs e)
    {
        if (_currentCandidate == null || _isAnimating) return;
        lblNopeOverlay.Opacity = 1;
        await AnimateCardOutAsync(false);
        await PerformLikeAsync(true);

        PerformReject();
    }

    private async void SuperLike_Clicked(object sender, EventArgs e)
    {
        if (_currentCandidate == null || _isAnimating) return;
        lblLikeOverlay.Opacity = 1;
        await AnimateCardOutAsync(true);
    }

    private async void Reload_Clicked(object sender, EventArgs e) => await LoadCandidates();

    // ── UI updaters ───────────────────────────────────────────────────────

    private async Task UpdateBestCompatibility(CandidateProfile candidate)
    {
        compatibilityBadge.IsVisible = false;
        compatibilityProgressBar.WidthRequest = 0;
        lblDetailCompatibility.IsVisible = false;

        if (!_sessionService.IsLoggedIn)
            return;

        var offers = _selectedJobOffer != null
                ? new List<JobOffer> { _selectedJobOffer }
                : await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);


        if (!offers.Any())
            return;

        var best = offers
            .Select(o => new { Offer = o, Score = _compatibilityService.CalculateScore(candidate, o) })
            .OrderByDescending(x => x.Score)
            .First();

        compatibilityBadge.IsVisible = true;
        lblCompatibility.Text = $"{best.Score}%";
        lblCompatibility.TextColor = Color.FromArgb("#7C4DFF");
        compatibilityProgressBar.WidthRequest = Math.Max(4, ProgressBarMaxWidth * best.Score / 100.0);

        lblDetailCompatibility.Text = $"Compatibilité : {best.Score}% avec « {best.Offer.Title} »";
        lblDetailCompatibility.IsVisible = true;
    }


    // Combines location + distance into one label "Nivelles · 12 km"
    private async Task UpdateDistanceAndLocation(CandidateProfile candidate)
    {
        lblDetailDistanceToOffer.IsVisible = false;

        if (!_sessionService.IsLoggedIn) return;

        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);

        var nearest = offers
            .Where(o => _distanceService.CanCalculateDistance(candidate, o))
            .Select(o => new { Offer = o, Km = _distanceService.CalculateDistanceKm(candidate, o) })
            .OrderBy(x => x.Km)
            .FirstOrDefault();

        if (nearest == null) return;

        string distPart = nearest.Km < 1 ? "< 1 km" : $"{Math.Round(nearest.Km)} km";

        // Combine into one label
        lblLocation.Text = $"{_currentLocationBase} · {distPart}";

        lblDetailDistanceToOffer.Text = $"Distance : {distPart} de « {nearest.Offer.Title} »";
        lblDetailDistanceToOffer.IsVisible = true;
    }

    private void UpdateCandidateBadges(CandidateProfile candidate)
    {
        skillsBadgesLayout.Children.Clear();

        var items = SplitValues(candidate.Skills).Take(2).ToList();
        if (!string.IsNullOrWhiteSpace(candidate.DesiredContractType))
            items.Add(candidate.DesiredContractType);

        skillsBadgesLayout.IsVisible = items.Any();

        foreach (var text in items)
        {
            skillsBadgesLayout.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#FAF9FF"),
                BorderColor = Color.FromArgb("#CFC4FF"),
                CornerRadius = 8,
                Padding = new Thickness(12, 5),
                HasShadow = false,
                Margin = new Thickness(0, 0, 8, 8),
                Content = new Label
                {
                    Text = text,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#5F4ACB")
                }
            });
        }
    }

    private void UpdateCandidatePreferences(CandidateProfile candidate)
    {
        string salary = GetSalaryDisplay(candidate);
        lblCandidatePreferences.Text = string.IsNullOrWhiteSpace(salary) ? "" : $"Salaire souhaité : {salary}";
        lblCandidatePreferences.IsVisible = !string.IsNullOrWhiteSpace(salary);
    }

    private void UpdateCandidateDetailFields(CandidateProfile candidate)
    {
        lblDetailSkills.Text = $"Compétences : {candidate.Skills}";
        lblDetailSkills.IsVisible = !string.IsNullOrWhiteSpace(candidate.Skills);

        lblDetailDesiredContractType.Text = $"Contrat recherché : {candidate.DesiredContractType}";
        lblDetailDesiredContractType.IsVisible = !string.IsNullOrWhiteSpace(candidate.DesiredContractType);

        lblDetailExperienceLevel.Text = $"Niveau : {candidate.ExperienceLevel}";
        lblDetailExperienceLevel.IsVisible = !string.IsNullOrWhiteSpace(candidate.ExperienceLevel);

        string salary = GetSalaryDisplay(candidate);
        lblDetailDesiredSalary.Text = $"Salaire souhaité : {salary}";
        lblDetailDesiredSalary.IsVisible = !string.IsNullOrWhiteSpace(salary);
    }

    private void UpdateExperienceSection(CandidateProfile candidate)
    {
        bool has1 = !string.IsNullOrWhiteSpace(candidate.ExperienceTitle1)
                 || !string.IsNullOrWhiteSpace(candidate.ExperienceCompany1);
        bool has2 = !string.IsNullOrWhiteSpace(candidate.ExperienceTitle2)
                 || !string.IsNullOrWhiteSpace(candidate.ExperienceCompany2);

        experienceSection.IsVisible = has1 || has2;
        experience1Layout.IsVisible = has1;
        lblExperienceTitle1.Text = candidate.ExperienceTitle1;
        lblExperienceCompany1.Text = string.IsNullOrWhiteSpace(candidate.ExperienceCompany1)
            ? "" : $"@ {candidate.ExperienceCompany1}";
        lblExperiencePeriod1.Text = candidate.ExperiencePeriod1;

        experience2Layout.IsVisible = has2;
        lblExperienceTitle2.Text = candidate.ExperienceTitle2;
        lblExperienceCompany2.Text = string.IsNullOrWhiteSpace(candidate.ExperienceCompany2)
            ? "" : $"@ {candidate.ExperienceCompany2}";
        lblExperiencePeriod2.Text = candidate.ExperiencePeriod2;
    }

    private void SetCandidateImage(CandidateProfile candidate)
    {
        imgCandidate.Source = !string.IsNullOrWhiteSpace(candidate.PhotoPath) && File.Exists(candidate.PhotoPath)
            ? ImageSource.FromFile(candidate.PhotoPath)
            : "candidate_default.jpg";
        imgCandidate.IsVisible = true;
        candidatePlaceholderIllustration.IsVisible = false;
    }

    // ── Offer filter ─────────────────────────────────────────────────────

    private async void OfferFilterPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isLoadingOfferFilter) return;
        _selectedJobOffer = offerFilterPicker.SelectedItem as JobOffer;
        await LoadCandidates();
    }

    private async void ClearOfferFilter_Clicked(object sender, EventArgs e)
    {
        _isLoadingOfferFilter = true;
        offerFilterPicker.SelectedItem = null;
        offerFilterPicker.SelectedIndex = -1;
        _selectedJobOffer = null;
        _isLoadingOfferFilter = false;
        await LoadCandidates();
    }

    // ── Match animation ───────────────────────────────────────────────────

    private async Task ShowPendingMatchAnimationIfNeeded()
    {
        if (!_sessionService.IsLoggedIn) return;

        var pending = await _matchRepository.GetPendingMatchAnimationAsync(_sessionService.CurrentUserId);
        if (pending == null) return;

        await _matchRepository.MarkMatchAnimationSeenAsync(pending.MatchId, _sessionService.CurrentUserId);
        await Shell.Current.GoToAsync(
            $"{nameof(MatchPage)}" +
            $"?participantId={pending.CandidateUserId}" +
            $"&participantName={Uri.EscapeDataString(pending.CandidateName)}");
    }

    // ── Navigation ────────────────────────────────────────────────────────

    private async void Back_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(RecruiterHomePage)}");

    private async void Home_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(RecruiterHomePage)}");

    private async void AddOffer_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));

    private async void Messages_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(MessagesPage));

    private async void Profile_Nav_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");

    private async void Profile_Tapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");

    // ── Helpers ───────────────────────────────────────────────────────────

    private string GetSalaryDisplay(CandidateProfile c)
    {
        if (c.DesiredSalaryMin > 0 && c.DesiredSalaryMax > 0) return $"{c.DesiredSalaryMin} - {c.DesiredSalaryMax} €";
        if (c.DesiredSalaryMin > 0) return $"À partir de {c.DesiredSalaryMin} €";
        if (c.DesiredSalaryMax > 0) return $"Jusqu'à {c.DesiredSalaryMax} €";
        return string.Empty;
    }

    private static List<string> SplitValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new();
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "AI";
        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][0].ToString().ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}