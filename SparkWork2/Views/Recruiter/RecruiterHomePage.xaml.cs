using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterHomePage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;

    private bool _isCheckingPendingMatch;

    public RecruiterHomePage(MatchRepository matchRepository, SessionService sessionService)
    {
        InitializeComponent();
        _matchRepository = matchRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isCheckingPendingMatch || !_sessionService.IsLoggedIn)
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
        await Shell.Current.GoToAsync(nameof(BrowseCandidatesPage));
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
}