using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterMatchesPage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;

    public RecruiterMatchesPage(
        MatchRepository matchRepository,
        SessionService sessionService)
    {
        InitializeComponent();

        _matchRepository = matchRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboard();
    }

    private async Task LoadDashboard()
    {
        if (!_sessionService.IsLoggedIn)
        {
            lblMatchesCount.Text = "0";
            lblNewMatchesText.Text = "0 nouveaux";
            matchesCollection.ItemsSource = new List<RecruiterMatchDashboardItem>();
            return;
        }

        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);

        var visibleMatches = matches
            .Where(match => match.RecruiterUserId == _sessionService.CurrentUserId)
            .Select(match => new RecruiterMatchDashboardItem
            {
                MatchId = match.MatchId,
                CandidateUserId = match.CandidateUserId,
                CandidateName = string.IsNullOrWhiteSpace(match.CandidateName)
                    ? "Candidat"
                    : match.CandidateName,
                JobTitle = string.IsNullOrWhiteSpace(match.JobTitle)
                    ? "Profil candidat"
                    : match.JobTitle,
                Initials = BuildInitials(match.CandidateName)
            })
            .ToList();

        lblMatchesCount.Text = visibleMatches.Count.ToString();
        lblNewMatchesText.Text = visibleMatches.Count == 1
            ? "1 nouveau"
            : $"{visibleMatches.Count} nouveaux";

        matchesCollection.ItemsSource = null;
        matchesCollection.ItemsSource = visibleMatches;
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async Task OpenConversationAsync(RecruiterMatchDashboardItem match)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(ConversationDetailPage)}" +
            $"?participantId={match.CandidateUserId}" +
            $"&participantName={Uri.EscapeDataString(match.CandidateName)}" +
            $"&returnRoute={nameof(RecruiterMatchesPage)}");
    }

    private async void Contact_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is RecruiterMatchDashboardItem match)
        {
            await OpenConversationAsync(match);
        }
    }

    private async void Match_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is RecruiterMatchDashboardItem match)
        {
            await OpenConversationAsync(match);
        }
    }

    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Profile_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }
    private async void Candidate_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is RecruiterMatchDashboardItem match)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(CandidateDetailPage)}?candidateId={match.CandidateUserId}");
        }
    }

}

public class RecruiterMatchDashboardItem
{
    public int MatchId { get; set; }
    public int CandidateUserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
}
