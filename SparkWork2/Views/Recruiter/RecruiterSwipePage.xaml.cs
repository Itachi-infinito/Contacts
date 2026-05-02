using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

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

    private double _panX;
    private bool _isExpanded;
    private bool _isAnimating;

    private const double SwipeThreshold = 120;

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

        await LoadRecruiterOffers();
        await LoadCandidates();
        await ShowPendingMatchAnimationIfNeeded();
    }


    private async Task LoadCandidates()
    {
        if (!_sessionService.IsLoggedIn)
        {
            ShowEmptyState("Connecte-toi pour voir les candidats.");
            return;
        }

        var allCandidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        var recruiterLikes = await _recruiterCandidateLikeRepository.GetLikesByRecruiterAsync(_sessionService.CurrentUserId);

        _candidates = allCandidates
            .Where(candidate => candidate.CandidateId != _sessionService.CurrentUserId)
            .Where(candidate => !recruiterLikes.Any(like => like.CandidateUserId == candidate.CandidateId))
            .ToList();
        if (_selectedJobOffer != null)
        {
            _candidates = _candidates
                .OrderByDescending(candidate =>
                    _compatibilityService.CalculateScore(candidate, _selectedJobOffer))
                .ToList();

            lblOfferFilterHint.Text =
                $"Trié par compatibilité avec {_selectedJobOffer.Title}.";
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
            ShowEmptyState("No more candidates right now.");
            return;
        }

        _currentCandidate = _candidates[_currentIndex];

        candidateCard.IsVisible = true;
        emptyStateSection.IsVisible = false;
        cardScrollView.IsVisible = true;
        lblSwipeHint.IsVisible = true;
        actionsGrid.IsVisible = true;



        var fullName = string.IsNullOrWhiteSpace(_currentCandidate.FullName)
            ? "Candidat"
            : _currentCandidate.FullName;

        var title = string.IsNullOrWhiteSpace(_currentCandidate.Title)
            ? "Profil candidat"
            : _currentCandidate.Title;

        var location = string.IsNullOrWhiteSpace(_currentCandidate.Location)
            ? "Localisation non renseignée"
            : _currentCandidate.Location;


        var about = string.IsNullOrWhiteSpace(_currentCandidate.About)
            ? "Profil candidat disponible pour une nouvelle opportunité."
            : _currentCandidate.About;

        lblFullName.Text = fullName;
        lblTitle.Text = title;
        lblLocation.Text = location;
        UpdateCandidateBadges(_currentCandidate);
        UpdateCandidateDetailFields(_currentCandidate);

        UpdateCandidatePreferences(_currentCandidate);
        _ = UpdateBestCompatibility(_currentCandidate);

        _ = UpdateDistanceToNearestOffer(_currentCandidate);


        lblDetailFullName.Text = fullName;
        lblDetailTitle.Text = title;
        lblDetailLocation.Text = location;
        lblAbout.Text = about;

        SetCandidateImage(_currentCandidate);
        aboutSection.IsVisible = true;


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

    private async Task ToggleDetailsAsync()
    {
        if (_currentCandidate == null || _isAnimating)
            return;

        _isExpanded = !_isExpanded;

        if (_isExpanded)
        {
            expandedDetailsSection.IsVisible = true;
            await expandedDetailsSection.FadeTo(1, 180);
        }
        else
        {
            await expandedDetailsSection.FadeTo(0, 150);
            expandedDetailsSection.IsVisible = false;
        }

        await cardScrollView.ScrollToAsync(candidateCard, ScrollToPosition.Start, true);
    }

    private async void ToggleDetails_Clicked(object sender, EventArgs e)
    {
        await ToggleDetailsAsync();
    }

    private void Card_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (_currentCandidate == null || _isAnimating)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                _panX = e.TotalX;

                candidateCard.TranslationX = e.TotalX;
                candidateCard.Rotation = e.TotalX / 20;

                if (e.TotalX > 0)
                {
                    lblLikeOverlay.Opacity = Math.Min(Math.Abs(e.TotalX) / 120.0, 1);
                    lblNopeOverlay.Opacity = 0;
                }
                else
                {
                    lblNopeOverlay.Opacity = Math.Min(Math.Abs(e.TotalX) / 120.0, 1);
                    lblLikeOverlay.Opacity = 0;
                }

                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _ = HandleSwipeCompletedAsync();
                break;
        }
    }

    private async Task HandleSwipeCompletedAsync()
    {
        if (_isAnimating)
            return;

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
            await ResetCardPositionAsync();
        }
    }

    private async Task AnimateCardOutAsync(bool toRight)
    {
        _isAnimating = true;

        var pageWidth = Width > 0 ? Width : 500;
        var targetX = toRight ? pageWidth + 260 : -pageWidth - 260;

        await Task.WhenAll(
            candidateCard.TranslateTo(targetX, 0, 240, Easing.CubicIn),
            candidateCard.RotateTo(toRight ? 20 : -20, 240, Easing.CubicIn),
            candidateCard.FadeTo(0, 220)
        );
    }

    private async Task ResetCardPositionAsync()
    {
        await Task.WhenAll(
            candidateCard.TranslateTo(0, 0, 180, Easing.SpringOut),
            candidateCard.RotateTo(0, 180, Easing.SpringOut),
            lblLikeOverlay.FadeTo(0, 120),
            lblNopeOverlay.FadeTo(0, 120)
        );

        _panX = 0;
    }

    private async Task PerformLikeAsync()
    {
        if (_currentCandidate == null || !_sessionService.IsLoggedIn)
        {
            _isAnimating = false;
            return;
        }

        var likedCandidate = _currentCandidate;

        var added = await _recruiterCandidateLikeRepository.AddLikeAsync(
            _sessionService.CurrentUserId,
            likedCandidate.CandidateId);

        var matchedOffer = await _candidateJobLikeRepository.GetFirstLikedOfferOfRecruiterAsync(
            likedCandidate.CandidateId,
            _sessionService.CurrentUserId);

        _candidates.RemoveAt(_currentIndex);

        if (added && matchedOffer != null)
        {
            await _matchRepository.AddMatchAsync(
                likedCandidate.CandidateId,
                likedCandidate.FullName,
                _sessionService.CurrentUserId,
                matchedOffer,
                false);

            await Shell.Current.GoToAsync(
                $"{nameof(MatchPage)}" +
                $"?participantId={likedCandidate.CandidateId}" +
                $"&participantName={Uri.EscapeDataString(likedCandidate.FullName)}");

            return;
        }

        ShowCurrentCandidate();
    }


    private void PerformReject()
    {
        if (_currentCandidate == null)
        {
            _isAnimating = false;
            return;
        }

        _candidates.RemoveAt(_currentIndex);
        ShowCurrentCandidate();
    }

    private async void Like_Clicked(object sender, EventArgs e)
    {
        if (_currentCandidate == null || _isAnimating)
            return;

        lblLikeOverlay.Opacity = 1;
        lblNopeOverlay.Opacity = 0;

        await AnimateCardOutAsync(true);
        await PerformLikeAsync();
    }

    private async void Reject_Clicked(object sender, EventArgs e)
    {
        if (_currentCandidate == null || _isAnimating)
            return;

        lblLikeOverlay.Opacity = 0;
        lblNopeOverlay.Opacity = 1;

        await AnimateCardOutAsync(false);
        PerformReject();
    }

    private async void Reload_Clicked(object sender, EventArgs e)
    {
        await LoadCandidates();
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterMatchesPage)}");
    }

    private async void Profile_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }
    private void SetCandidateImage(CandidateProfile candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate.PhotoPath) && File.Exists(candidate.PhotoPath))
        {
            imgCandidate.Source = ImageSource.FromFile(candidate.PhotoPath);
            imgCandidate.Opacity = 1;
            imgCandidate.IsVisible = true;
            candidatePlaceholderIllustration.IsVisible = false;
        }
        else
        {
            imgCandidate.Source = null;
            imgCandidate.Opacity = 0;
            imgCandidate.IsVisible = false;
            candidatePlaceholderIllustration.IsVisible = true;
        }
    }

    private async Task ShowPendingMatchAnimationIfNeeded()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var pendingMatch = await _matchRepository.GetPendingMatchAnimationAsync(_sessionService.CurrentUserId);

        if (pendingMatch == null)
            return;

        await _matchRepository.MarkMatchAnimationSeenAsync(
            pendingMatch.MatchId,
            _sessionService.CurrentUserId);

        await Shell.Current.GoToAsync(
            $"{nameof(MatchPage)}" +
            $"?participantId={pendingMatch.CandidateUserId}" +
            $"&participantName={Uri.EscapeDataString(pendingMatch.CandidateName)}");
    }
    private void UpdateCandidateBadges(CandidateProfile candidate)
    {
        skillsBadgesLayout.Children.Clear();

        var skills = SplitValues(candidate.Skills)
            .Take(4)
            .ToList();

        skillsBadgesLayout.IsVisible = skills.Any();

        foreach (var skill in skills)
        {
            var badge = new Frame
            {
                BackgroundColor = Color.FromArgb("#F0EAFE"),
                CornerRadius = 12,
                Padding = new Thickness(9, 3),
                HasShadow = false,
                Margin = new Thickness(0, 0, 8, 8),
                Content = new Label
                {
                    Text = skill,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#7C4DFF")
                }
            };

            skillsBadgesLayout.Children.Add(badge);
        }
    }

    private void UpdateCandidatePreferences(CandidateProfile candidate)
    {
        var parts = new List<string>();

        string salary = GetDesiredSalaryDisplay(candidate);
        if (!string.IsNullOrWhiteSpace(salary))
            parts.Add($"Salaire souhaité : {salary}");

        if (!string.IsNullOrWhiteSpace(candidate.DesiredContractType))
            parts.Add(candidate.DesiredContractType);

        if (!string.IsNullOrWhiteSpace(candidate.ExperienceLevel))
            parts.Add(candidate.ExperienceLevel);

        lblCandidatePreferences.Text = string.Join(" · ", parts);
        lblCandidatePreferences.IsVisible = parts.Any();
    }

    private void UpdateCandidateDetailFields(CandidateProfile candidate)
    {
        lblDetailSkills.Text = $"Compétences : {candidate.Skills}";
        lblDetailSkills.IsVisible = !string.IsNullOrWhiteSpace(candidate.Skills);

        lblDetailDesiredContractType.Text = $"Contrat recherché : {candidate.DesiredContractType}";
        lblDetailDesiredContractType.IsVisible = !string.IsNullOrWhiteSpace(candidate.DesiredContractType);

        lblDetailExperienceLevel.Text = $"Niveau : {candidate.ExperienceLevel}";
        lblDetailExperienceLevel.IsVisible = !string.IsNullOrWhiteSpace(candidate.ExperienceLevel);

        string salary = GetDesiredSalaryDisplay(candidate);
        lblDetailDesiredSalary.Text = $"Salaire souhaité : {salary}";
        lblDetailDesiredSalary.IsVisible = !string.IsNullOrWhiteSpace(salary);
    }

    private string GetDesiredSalaryDisplay(CandidateProfile candidate)
    {
        if (candidate.DesiredSalaryMin > 0 && candidate.DesiredSalaryMax > 0)
            return $"{candidate.DesiredSalaryMin} - {candidate.DesiredSalaryMax} €";

        if (candidate.DesiredSalaryMin > 0)
            return $"À partir de {candidate.DesiredSalaryMin} €";

        if (candidate.DesiredSalaryMax > 0)
            return $"Jusqu'à {candidate.DesiredSalaryMax} €";

        return string.Empty;
    }

    private List<string> SplitValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    private async Task UpdateDistanceToNearestOffer(CandidateProfile candidate)
    {
        lblDistanceToOffer.IsVisible = false;
        lblDetailDistanceToOffer.IsVisible = false;

        if (!_sessionService.IsLoggedIn)
            return;

        var recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(
            _sessionService.CurrentUserId);

        var offersWithDistance = recruiterOffers
            .Where(offer => _distanceService.CanCalculateDistance(candidate, offer))
            .Select(offer => new
            {
                Offer = offer,
                Distance = _distanceService.CalculateDistanceKm(candidate, offer)
            })
            .OrderBy(x => x.Distance)
            .ToList();

        if (!offersWithDistance.Any())
            return;

        var nearest = offersWithDistance.First();
        string distanceText = nearest.Distance < 1
            ? $"À moins de 1 km de l'offre {nearest.Offer.Title}"
            : $"À environ {Math.Round(nearest.Distance)} km de l'offre {nearest.Offer.Title}";

        lblDistanceToOffer.Text = distanceText;
        lblDistanceToOffer.IsVisible = true;

        lblDetailDistanceToOffer.Text = $"Distance : {distanceText}";
        lblDetailDistanceToOffer.IsVisible = true;
    }
    private async Task UpdateBestCompatibility(CandidateProfile candidate)
    {
        compatibilityBadge.IsVisible = false;
        lblDetailCompatibility.IsVisible = false;

        if (!_sessionService.IsLoggedIn)
            return;

        var recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(
            _sessionService.CurrentUserId);

        if (!recruiterOffers.Any())
            return;

        var bestMatch = recruiterOffers
            .Select(offer => new
            {
                Offer = offer,
                Score = _compatibilityService.CalculateScore(candidate, offer)
            })
            .OrderByDescending(x => x.Score)
            .First();

        compatibilityBadge.IsVisible = true;
        lblCompatibility.Text = $"Compatibilité {bestMatch.Score}%";

        lblDetailCompatibility.Text =
            $"Meilleure compatibilité : {bestMatch.Score}% avec l'offre {bestMatch.Offer.Title}";

        lblDetailCompatibility.IsVisible = true;

        if (bestMatch.Score >= 75)
        {
            compatibilityBadge.BackgroundColor = Color.FromArgb("#ECFDF5");
            lblCompatibility.TextColor = Color.FromArgb("#10B981");
        }
        else if (bestMatch.Score >= 45)
        {
            compatibilityBadge.BackgroundColor = Color.FromArgb("#F0EAFE");
            lblCompatibility.TextColor = Color.FromArgb("#7C4DFF");
        }
        else
        {
            compatibilityBadge.BackgroundColor = Color.FromArgb("#FFF1F2");
            lblCompatibility.TextColor = Color.FromArgb("#E11D48");
        }
    }
    private async Task LoadRecruiterOffers()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        _isLoadingOfferFilter = true;

        _recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(
            _sessionService.CurrentUserId);

        offerFilterPicker.ItemsSource = null;
        offerFilterPicker.ItemsSource = _recruiterOffers;

        offerFilterCard.IsVisible = _recruiterOffers.Any();
        lblOfferFilterHint.Text = _recruiterOffers.Any()
            ? "Choisis une offre pour trier les candidats."
            : "Crée une offre pour obtenir un score plus précis.";

        _isLoadingOfferFilter = false;
    }

    private async void OfferFilterPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isLoadingOfferFilter)
            return;

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

        lblOfferFilterHint.Text = "Toutes les offres sont prises en compte.";
        await LoadCandidates();
    }

}
