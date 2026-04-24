using System.Linq;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Candidate;

public partial class CandidateSwipePage : ContentPage
{
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly MatchRepository _matchRepository;
    private readonly RecruiterCandidateLikeRepository _recruiterCandidateLikeRepository;
    private readonly SessionService _sessionService;

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
        SessionService sessionService)
    {
        InitializeComponent();

        _candidateJobLikeRepository = candidateJobLikeRepository;
        _jobOfferRepository = jobOfferRepository;
        _matchRepository = matchRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadJobOffers();
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

            lblTitle.Text = "You're all caught up ✨";
            lblCompany.Text = "";
            lblLocation.Text = "";
            lblContractType.Text = "";
            lblDescription.Text = "";
            lblDetailCompany.Text = "";
            lblDetailLocation.Text = "";
            lblDetailContractType.Text = "";
            lblEmptyMessage.Text = "No more opportunities right now.";
            imgJob.Source = "dotnet_bot.png";

            contractTypeBadge.IsVisible = false;
            btnImageExpandCollapse.IsVisible = false;
            btnHeaderExpandCollapse.IsVisible = false;
            descriptionSection.IsVisible = false;
            expandedDetailsSection.IsVisible = false;
            emptyStateSection.IsVisible = true;
            return;
        }

        _currentJobOffer = _jobOffers[_currentIndex];

        lblTitle.Text = _currentJobOffer.Title;
        lblCompany.Text = _currentJobOffer.CompanyName;
        lblLocation.Text = _currentJobOffer.Location;
        lblContractType.Text = _currentJobOffer.ContractType;
        lblDescription.Text = _currentJobOffer.Description;

        lblDetailCompany.Text = _currentJobOffer.CompanyName;
        lblDetailLocation.Text = _currentJobOffer.Location;
        lblDetailContractType.Text = _currentJobOffer.ContractType;

        imgJob.Source = "dotnet_bot.png";

        contractTypeBadge.IsVisible = !string.IsNullOrWhiteSpace(_currentJobOffer.ContractType);
        btnImageExpandCollapse.IsVisible = true;
        btnHeaderExpandCollapse.IsVisible = false;
        descriptionSection.IsVisible = true;
        expandedDetailsSection.IsVisible = false;
        emptyStateSection.IsVisible = false;
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

        btnHeaderExpandCollapse.IsVisible = false;
        btnHeaderExpandCollapse.Rotation = 0;
    }

    private async Task ToggleDetailsAsync()
    {
        if (_currentJobOffer == null)
            return;

        if (!_isExpanded)
        {
            _isExpanded = true;

            btnImageExpandCollapse.IsVisible = false;
            btnHeaderExpandCollapse.IsVisible = true;
            btnHeaderExpandCollapse.Rotation = 180;

            expandedDetailsSection.IsVisible = true;
            await expandedDetailsSection.FadeTo(1, 180);
            await cardScrollView.ScrollToAsync(jobCard, ScrollToPosition.Start, true);
        }
        else
        {
            _isExpanded = false;

            await expandedDetailsSection.FadeTo(0, 150);
            expandedDetailsSection.IsVisible = false;

            btnHeaderExpandCollapse.Rotation = 0;
            btnHeaderExpandCollapse.IsVisible = false;

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
                likedOffer.RecruiterId,
                _sessionService.CurrentUserId);

        _jobOffers.RemoveAt(_currentIndex);
        ShowCurrentJobOffer();

        if (added && recruiterLikedCandidate)
        {
            await _matchRepository.AddMatchAsync(
                _sessionService.CurrentUserId,
                _sessionService.CurrentUserName,
                likedOffer.RecruiterId,
                likedOffer,
                true);

            await Shell.Current.GoToAsync(
                $"{nameof(MatchPage)}" +
                $"?participantId={likedOffer.RecruiterId}" +
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
}