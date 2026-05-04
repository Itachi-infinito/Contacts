using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Candidate;

public partial class CandidateHomePage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;

    private bool _isCheckingPendingMatch;

    public CandidateHomePage(MatchRepository matchRepository, SessionService sessionService)
    {
        InitializeComponent();
        _matchRepository = matchRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        lblGreeting.Text = $"Bonjour, {_sessionService.CurrentUserName}";
        lblInitials.Text = BuildInitials(_sessionService.CurrentUserName);

        base.OnAppearing();

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

    private async void BrowseJobOffers_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateSwipePage));

    }


    private async void Matches_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MatchesPage));
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
    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "ME";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }


}