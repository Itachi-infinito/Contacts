using Microsoft.Extensions.Logging;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;
using System.Linq;

namespace SparkWork2.Views.Candidate;

public partial class CandidateSwipePage : ContentPage
{
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly MatchRepository _matchRepository;
    private readonly RecruiterCandidateLikeRepository _recruiterCandidateLikeRepository;
    private readonly SessionService _sessionService;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly CompatibilityService _compatibilityService;
    private readonly DistanceService _distanceService;



    private List<JobOffer> _jobOffers = new();
    private int _currentIndex = 0;
    private JobOffer? _currentJobOffer;

    private double _panX;
    private const double SwipeThreshold = 120;
    private bool _isExpanded = false;

    public CandidateSwipePage(
        CandidateJobLikeRepository candidateJobLikeRepository,
        JobOfferRepository jobOfferRepository,
        MatchRepository matchRepository,
        RecruiterCandidateLikeRepository recruiterCandidateLikeRepository,
        CandidateProfileRepository candidateProfileRepository,
        CompatibilityService compatibilityService,
        DistanceService distanceService,
        SessionService sessionService)

    {
        InitializeComponent();

        _candidateJobLikeRepository = candidateJobLikeRepository;
        _jobOfferRepository = jobOfferRepository;
        _matchRepository = matchRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _sessionService = sessionService;
        _candidateProfileRepository = candidateProfileRepository;
        _compatibilityService = compatibilityService;
        _distanceService = distanceService;


    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await LoadJobOffers();
            await ShowPendingMatchAnimationIfNeeded();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }



    private async Task LoadJobOffers()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var allOffers = await _jobOfferRepository.GetJobOffersAsync();
        var likedOffers = await _candidateJobLikeRepository.GetLikesByCandidateAsync(_sessionService.CurrentUserId);

        var likedJobOfferIds = likedOffers
            .Select(x => x.JobOfferId)
            .ToList();

        _jobOffers = allOffers
            .Where(x => !likedJobOfferIds.Contains(x.JobOfferId))
            .ToList();

        _currentIndex = 0;
        ShowCurrentJobOffer();
    }

    private void ShowCurrentJobOffer()
    {
        ResetCardVisuals();
        ResetExpandedState();

        if (_jobOffers == null || !_jobOffers.Any() || _currentIndex >= _jobOffers.Count)
        {
            _currentJobOffer = null;

            lblEmptyMessage.Text = "No more opportunities right now.";

            jobCard.IsVisible = false;
            emptyStateSection.IsVisible = true;
            lblSwipeHint.IsVisible = false;
            actionsGrid.IsVisible = false;

            contractTypeBadge.IsVisible = false;
            levelBadge.IsVisible = false;
            remoteBadge.IsVisible = false;
            skillsPreviewCard.IsVisible = false;
            lblSalary.IsVisible = false;

            btnImageExpandCollapse.IsVisible = false;
            descriptionSection.IsVisible = false;
            expandedDetailsSection.IsVisible = false;

            return;
        }

        _currentJobOffer = _jobOffers[_currentIndex];
        var currentOffer = _currentJobOffer;

        jobCard.IsVisible = true;
        emptyStateSection.IsVisible = false;
        lblSwipeHint.IsVisible = true;
        actionsGrid.IsVisible = true;

        lblTitle.Text = currentOffer.Title;
        lblCompany.Text = currentOffer.CompanyName;
        lblCompanyLogoInitials.Text = BuildInitials(currentOffer.CompanyName);
        lblLocation.Text = currentOffer.Location;
        lblDescription.Text = currentOffer.Description;
        lblShortDescription.Text = currentOffer.Description;
        lblShortDescription.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.Description);
        



        lblSalary.Text = GetSalaryDisplay(currentOffer);
        lblSalary.IsVisible = currentOffer.SalaryMin > 0 || currentOffer.SalaryMax > 0;
        _ = UpdateCompatibilityBadge(currentOffer);
        _ = UpdateDistanceLabel(currentOffer);



        contractTypeBadge.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.ContractType);
        lblContractType.Text = currentOffer.ContractType;

        levelBadge.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.Level);
        lblLevel.Text = currentOffer.Level;

        remoteBadge.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.RemoteMode);
        lblRemoteMode.Text = currentOffer.RemoteMode;

        string skillsPreview = BuildSkillsPreview(currentOffer);

        skillsPreviewCard.IsVisible = !string.IsNullOrWhiteSpace(skillsPreview);
        lblSkillsPreview.Text = skillsPreview;


        lblDetailCompany.Text = $"Entreprise : {currentOffer.CompanyName}";
        lblDetailLocation.Text = $"Lieu : {currentOffer.Location}";
        lblDetailContractType.Text = $"Contrat : {currentOffer.ContractType}";

        lblDetailSalary.Text = $"Salaire : {GetSalaryDisplay(currentOffer)}";
        lblDetailSalary.IsVisible = currentOffer.SalaryMin > 0 || currentOffer.SalaryMax > 0;

        lblDetailLevel.Text = $"Niveau : {currentOffer.Level}";
        lblDetailLevel.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.Level);

        lblDetailRemoteMode.Text = $"Mode : {currentOffer.RemoteMode}";
        lblDetailRemoteMode.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.RemoteMode);

        requiredSkillsDetailSection.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.RequiredSkills);
        lblDetailRequiredSkills.Text = currentOffer.RequiredSkills;

        niceSkillsDetailSection.IsVisible = !string.IsNullOrWhiteSpace(currentOffer.NiceToHaveSkills);
        lblDetailNiceToHaveSkills.Text = currentOffer.NiceToHaveSkills;

        //imgJob.Source = null;
        //imgJob.IsVisible = false;
        //imgJob.Opacity = 0;
        imgJob.Source = "horeca_default.jpg";
        imgJob.IsVisible = true;
        imgJob.Opacity = 1;

        btnImageExpandCollapse.IsVisible = true;
        descriptionSection.IsVisible = true;
        expandedDetailsSection.IsVisible = false;
    }



    private void ResetCardVisuals()
    {
        _panX = 0;
        jobCard.TranslationX = 0;
        jobCard.TranslationY = 0;
        jobCard.Rotation = 0;
        lblLikeOverlay.Opacity = 0;
        lblNopeOverlay.Opacity = 0;
    }

    private void ResetExpandedState()
    {
        _isExpanded = false;

        expandedDetailsSection.IsVisible = false;
        expandedDetailsSection.Opacity = 0;

        btnImageExpandCollapse.IsVisible = true;
        btnImageExpandCollapse.Rotation = 0;

    }

    private async Task ToggleDetailsAsync()
    {
        if (_currentJobOffer == null)
            return;

        if (!_isExpanded)
        {
            _isExpanded = true;

            btnImageExpandCollapse.IsVisible = false;

            expandedDetailsSection.IsVisible = true;
            await expandedDetailsSection.FadeTo(1, 180);
            await cardScrollView.ScrollToAsync(jobCard, ScrollToPosition.Start, true);
        }
        else
        {
            _isExpanded = false;

            await expandedDetailsSection.FadeTo(0, 150);
            expandedDetailsSection.IsVisible = false;


            btnImageExpandCollapse.IsVisible = true;
            await cardScrollView.ScrollToAsync(jobCard, ScrollToPosition.Start, true);
        }
    }

    private async void ToggleDetails_Clicked(object sender, EventArgs e)
    {
        await ToggleDetailsAsync();
    }

    private void Card_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (_currentJobOffer == null)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                _panX = e.TotalX;

                jobCard.TranslationX = e.TotalX;
                jobCard.Rotation = e.TotalX / 20;

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
            await jobCard.TranslateTo(0, 0, 180, Easing.SpringOut);
            await jobCard.RotateTo(0, 180, Easing.SpringOut);
            lblLikeOverlay.Opacity = 0;
            lblNopeOverlay.Opacity = 0;
        }
    }

    private async Task AnimateCardOutAsync(bool toRight)
    {
        double targetX = toRight ? 500 : -500;

        await Task.WhenAll(
            jobCard.TranslateTo(targetX, 0, 220, Easing.CubicIn),
            jobCard.RotateTo(toRight ? 20 : -20, 220, Easing.CubicIn)
        );
    }

    private async Task PerformLikeAsync()
    {
        if (_currentJobOffer == null || !_sessionService.IsLoggedIn)
            return;

        var likedOffer = _currentJobOffer;

        bool added = await _candidateJobLikeRepository.AddLikeAsync(
            _sessionService.CurrentUserId,
            likedOffer.JobOfferId);

        bool recruiterLikedCandidate =
            await _recruiterCandidateLikeRepository.HasRecruiterLikedCandidateAsync(
                likedOffer.RecruiterUserId,
                _sessionService.CurrentUserId);

        _jobOffers.RemoveAt(_currentIndex);
        ShowCurrentJobOffer();

        if (added && recruiterLikedCandidate)
        {
            await _matchRepository.AddMatchAsync(
                _sessionService.CurrentUserId,
                _sessionService.CurrentUserName,
                likedOffer.RecruiterUserId,
                likedOffer,
                true);

            await Shell.Current.GoToAsync(
                $"{nameof(MatchPage)}" +
                $"?participantId={likedOffer.RecruiterUserId}" +
                $"&participantName={Uri.EscapeDataString(likedOffer.CompanyName)}");
        }
    }

    private void PerformReject()
    {
        if (_currentJobOffer == null)
            return;

        _jobOffers.RemoveAt(_currentIndex);
        ShowCurrentJobOffer();
    }

    private async void Like_Clicked(object sender, EventArgs e)
    {
        if (_currentJobOffer == null)
            return;

        lblLikeOverlay.Opacity = 1;
        await AnimateCardOutAsync(true);
        await PerformLikeAsync();
    }

    private async void Reject_Clicked(object sender, EventArgs e)
    {
        if (_currentJobOffer == null)
            return;

        lblNopeOverlay.Opacity = 1;
        await AnimateCardOutAsync(false);
        PerformReject();
    }

    private async void Reload_Clicked(object sender, EventArgs e)
    {
        await LoadJobOffers();
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
    private async void Discover_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateSwipePage));
    }


    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MatchesPage)}");
    }

    private async void Profile_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateProfilePage)}");
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
            $"?participantId={pendingMatch.RecruiterUserId}" +
            $"&participantName={Uri.EscapeDataString(pendingMatch.CompanyName)}");
    }

    private string GetSalaryDisplay(JobOffer jobOffer)
    {
        if (jobOffer.SalaryMin > 0 && jobOffer.SalaryMax > 0)
            return $"{jobOffer.SalaryMin} - {jobOffer.SalaryMax} €";

        if (jobOffer.SalaryMin > 0)
            return $"À partir de {jobOffer.SalaryMin} €";

        if (jobOffer.SalaryMax > 0)
            return $"Jusqu'à {jobOffer.SalaryMax} €";

        return string.Empty;
    }
    private async Task UpdateCompatibilityBadge(JobOffer currentOffer)
    {
        try
        {
            if (!_sessionService.IsLoggedIn)
            {
                compatibilityBadge.IsVisible = false;
                return;
            }

            var candidateProfile = await _candidateProfileRepository.GetCandidateProfileAsync(
                _sessionService.CurrentUserId,
                _sessionService.CurrentUserName,
                _sessionService.CurrentUserEmail);

            int score = _compatibilityService.CalculateScore(candidateProfile, currentOffer);

            compatibilityBadge.IsVisible = true;
            lblCompatibility.Text = $"Compatibilité {score}%";
            matchTipCard.IsVisible = true;

            lblMatchTip.Text = score >= 75
                ? "Votre profil correspond très bien à cette offre."
                : score >= 45
                    ? "Cette offre peut vous correspondre, surtout si vous complétez vos compétences."
                    : "Cette offre est moins proche de votre profil actuel.";


            if (score >= 75)
            {
                compatibilityBadge.BackgroundColor = Color.FromArgb("#ECFDF5");
                lblCompatibility.TextColor = Color.FromArgb("#10B981");
            }
            else if (score >= 45)
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
        catch (Exception ex)
        {
            compatibilityBadge.IsVisible = false;
            System.Diagnostics.Debug.WriteLine($"Compatibility error: {ex}");
        }
    }

    private async Task UpdateDistanceLabel(JobOffer currentOffer)
    {
        try
        {
            if (!_sessionService.IsLoggedIn)
            {
                lblDistance.IsVisible = false;
                return;
            }

            var candidateProfile = await _candidateProfileRepository.GetCandidateProfileAsync(
                _sessionService.CurrentUserId,
                _sessionService.CurrentUserName,
                _sessionService.CurrentUserEmail);

            string distanceDisplay = _distanceService.GetDistanceDisplay(candidateProfile, currentOffer);

            lblDistance.IsVisible = !string.IsNullOrWhiteSpace(distanceDisplay);
            lblDistance.Text = distanceDisplay;
        }
        catch (Exception ex)
        {
            lblDistance.IsVisible = false;
            System.Diagnostics.Debug.WriteLine($"Distance error: {ex}");
        }
    }

    private async void Home_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateHomePage)}");
    }

    private async void Messages_Button_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Profile_Button_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateProfilePage)}");
    }

    private static string BuildSkillsPreview(JobOffer offer)
    {
        var source = !string.IsNullOrWhiteSpace(offer.RequiredSkills)
            ? offer.RequiredSkills
            : offer.NiceToHaveSkills;

        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        var skills = source
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .ToList();

        var visibleSkills = skills.Take(3).ToList();

        if (skills.Count > 3)
            return $"{string.Join(", ", visibleSkills)} +{skills.Count - 3}";

        return string.Join(", ", visibleSkills);
    }


    private async void Image_Tapped(object sender, TappedEventArgs e)
    {
        await ToggleDetailsAsync();
    }
    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "SW";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
 

}