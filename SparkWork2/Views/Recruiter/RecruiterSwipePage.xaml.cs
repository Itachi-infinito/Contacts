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

    private List<CandidateProfile> _candidates = new();
    private int _currentIndex = 0;
    private CandidateProfile? _currentCandidate;

    private double _panX;
    private const double SwipeThreshold = 120;
    private bool _isExpanded = false;

    public RecruiterSwipePage(
        CandidateJobLikeRepository candidateJobLikeRepository,
        JobOfferRepository jobOfferRepository,
        MatchRepository matchRepository,
        RecruiterCandidateLikeRepository recruiterCandidateLikeRepository,
        CandidateProfileRepository candidateProfileRepository,
        SessionService sessionService)
    {
        InitializeComponent();

        _candidateJobLikeRepository = candidateJobLikeRepository;
        _jobOfferRepository = jobOfferRepository;
        _matchRepository = matchRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _candidateProfileRepository = candidateProfileRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCandidates();
    }

    private async Task LoadCandidates()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var allCandidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        var recruiterLikes = await _recruiterCandidateLikeRepository.GetLikesByRecruiterAsync(_sessionService.CurrentUserId);

        _candidates = allCandidates
            .Where(x => x.CandidateId != _sessionService.CurrentUserId)
            .Where(x => !recruiterLikes.Any(like => like.CandidateUserId == x.CandidateId))
            .ToList();

        _currentIndex = 0;
        ShowCurrentCandidate();
    }

    private void ShowCurrentCandidate()
    {
        ResetCardVisuals();
        ResetExpandedState();

        if (_candidates == null || !_candidates.Any() || _currentIndex >= _candidates.Count)
        {
            _currentCandidate = null;

            lblFullName.Text = "You're all caught up ✨";
            lblTitle.Text = "";
            lblLocation.Text = "";
            lblAbout.Text = "";

            lblDetailFullName.Text = "";
            lblDetailTitle.Text = "";
            lblDetailLocation.Text = "";
            lblAbout.Text = "";
            lblEmptyMessage.Text = "No more candidates right now.";
            imgCandidate.Source = "dotnet_bot.png";

            btnImageExpandCollapse.IsVisible = false;
            btnHeaderExpandCollapse.IsVisible = false;
            aboutSection.IsVisible = false;
            emptyStateSection.IsVisible = true;
            return;
        }

        _currentCandidate = _candidates[_currentIndex];

        lblFullName.Text = _currentCandidate.FullName;
        lblTitle.Text = _currentCandidate.Title;
        lblLocation.Text = _currentCandidate.Location;
        lblAbout.Text = _currentCandidate.About;

        lblDetailFullName.Text = _currentCandidate.FullName;
        lblDetailTitle.Text = _currentCandidate.Title;
        lblDetailLocation.Text = _currentCandidate.Location;
        lblAbout.Text = _currentCandidate.About;

        imgCandidate.Source = "dotnet_bot.png";

        btnImageExpandCollapse.IsVisible = true;
        btnHeaderExpandCollapse.IsVisible = false;
        aboutSection.IsVisible = true;
        emptyStateSection.IsVisible = false;
    }

    private void ResetCardVisuals()
    {
        _panX = 0;
        candidateCard.TranslationX = 0;
        candidateCard.TranslationY = 0;
        candidateCard.Rotation = 0;
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

        btnHeaderExpandCollapse.IsVisible = false;
        btnHeaderExpandCollapse.Rotation = 0;
    }

    private async Task ToggleDetailsAsync()
    {
        if (_currentCandidate == null)
            return;

        if (!_isExpanded)
        {
            _isExpanded = true;

            btnImageExpandCollapse.IsVisible = false;
            btnHeaderExpandCollapse.IsVisible = true;
            btnHeaderExpandCollapse.Rotation = 180;

            expandedDetailsSection.IsVisible = true;
            await expandedDetailsSection.FadeTo(1, 180);
            await cardScrollView.ScrollToAsync(candidateCard, ScrollToPosition.Start, true);
        }
        else
        {
            _isExpanded = false;

            await expandedDetailsSection.FadeTo(0, 150);
            expandedDetailsSection.IsVisible = false;

            btnHeaderExpandCollapse.Rotation = 0;
            btnHeaderExpandCollapse.IsVisible = false;
            btnImageExpandCollapse.IsVisible = true;

            await cardScrollView.ScrollToAsync(candidateCard, ScrollToPosition.Start, true);
        }
    }

    private async void ToggleDetails_Clicked(object sender, EventArgs e)
    {
        await ToggleDetailsAsync();
    }

    private void Card_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (_currentCandidate == null)
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
            await candidateCard.TranslateTo(0, 0, 180, Easing.SpringOut);
            await candidateCard.RotateTo(0, 180, Easing.SpringOut);
            lblLikeOverlay.Opacity = 0;
            lblNopeOverlay.Opacity = 0;
        }
    }

    private async Task AnimateCardOutAsync(bool toRight)
    {
        double targetX = toRight ? 500 : -500;

        await Task.WhenAll(
            candidateCard.TranslateTo(targetX, 0, 220, Easing.CubicIn),
            candidateCard.RotateTo(toRight ? 20 : -20, 220, Easing.CubicIn)
        );
    }

    private async Task PerformLikeAsync()
    {
        if (_currentCandidate == null || !_sessionService.IsLoggedIn)
            return;

        var shortlistedCandidate = _currentCandidate;

        bool added = await _recruiterCandidateLikeRepository.AddLikeAsync(
            _sessionService.CurrentUserId,
            shortlistedCandidate.CandidateId);

        bool candidateLikedRecruiter =
            await _candidateJobLikeRepository.HasCandidateLikedAnyOfferOfRecruiterAsync(
                shortlistedCandidate.CandidateId,
                _sessionService.CurrentUserId);

        _candidates.RemoveAt(_currentIndex);
        ShowCurrentCandidate();

        if (added && candidateLikedRecruiter)
        {
            var recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);
            var firstOffer = recruiterOffers.FirstOrDefault();

            if (firstOffer != null)
            {
                await _matchRepository.AddMatchAsync(
                    shortlistedCandidate.CandidateId,
                    shortlistedCandidate.FullName,
                    _sessionService.CurrentUserId,
                    firstOffer,
                    false);
            }

            await Shell.Current.GoToAsync(
                $"{nameof(MatchPage)}" +
                $"?participantId={shortlistedCandidate.CandidateId}" +
                $"&participantName={Uri.EscapeDataString(shortlistedCandidate.FullName)}");
        }
    }

    private void PerformReject()
    {
        if (_currentCandidate == null)
            return;

        _candidates.RemoveAt(_currentIndex);
        ShowCurrentCandidate();
    }

    private async void Like_Clicked(object sender, EventArgs e)
    {
        if (_currentCandidate == null)
            return;

        lblLikeOverlay.Opacity = 1;
        await AnimateCardOutAsync(true);
        await PerformLikeAsync();
    }

    private async void Reject_Clicked(object sender, EventArgs e)
    {
        if (_currentCandidate == null)
            return;

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
}