using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Candidate;

public partial class MatchesPage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;

    public MatchesPage(
        MatchRepository matchRepository,
        JobOfferRepository jobOfferRepository,
        SessionService sessionService)
    {
        InitializeComponent();

        _matchRepository = matchRepository;
        _jobOfferRepository = jobOfferRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMatches();
    }

    private async Task LoadMatches()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        int candidateUserId = _sessionService.CurrentUserId;

        var matches = await _matchRepository.GetMatchesAsync(candidateUserId);
        var visibleMatches = new List<CandidateMatchDashboardItem>();

        foreach (var match in matches)
        {
            var offer = await _jobOfferRepository.GetJobOfferByIdAsync(match.JobOfferId);
            if (offer == null)
                continue;

            visibleMatches.Add(new CandidateMatchDashboardItem
            {
                MatchId = match.MatchId,
                JobOfferId = match.JobOfferId,
                RecruiterUserId = match.RecruiterUserId,
                CompanyName = offer.CompanyName,
                JobTitle = offer.Title,
                Initials = BuildInitials(offer.CompanyName)
            });
        }

        matchesCollection.ItemsSource = null;
        matchesCollection.ItemsSource = visibleMatches;

        lblMatchesCount.Text = visibleMatches.Count.ToString();
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        var parts = value.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0].Length >= 2
                ? parts[0][..2].ToUpperInvariant()
                : parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
    }

    private async void Match_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is CandidateMatchDashboardItem selectedMatch)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(JobOfferDetailPage)}?id={selectedMatch.JobOfferId}");
        }
    }

    private async void Contact_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not CandidateMatchDashboardItem match)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(ConversationDetailPage)}" +
            $"?participantId={match.RecruiterUserId}" +
            $"&participantName={Uri.EscapeDataString(match.CompanyName)}");
    }

    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateSwipePage)}");
    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Profile_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateProfilePage)}");
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void SwipeBubble_Loaded(object sender, EventArgs e)
    {
        if (sender is not VisualElement bubble)
            return;

        if (Math.Abs(bubble.Scale - 1) < 0.01)
            return;

        await bubble.ScaleTo(1, 220, Easing.SpringOut);
    }

    private async void MatchCard_Loaded(object sender, EventArgs e)
    {
        if (sender is not VisualElement card)
            return;

        if (card.Opacity >= 0.99 &&
            Math.Abs(card.TranslationY) < 0.1 &&
            Math.Abs(card.Scale - 1) < 0.01)
            return;

        await Task.WhenAll(
            card.FadeTo(1, 180, Easing.CubicOut),
            card.TranslateTo(0, 0, 240, Easing.CubicOut),
            card.ScaleTo(1, 260, Easing.SpringOut)
        );
    }
    private async void Offer_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is CandidateMatchDashboardItem match)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(JobOfferDetailPage)}?id={match.JobOfferId}");
        }
    }

}

public class CandidateMatchDashboardItem
{
    public int MatchId { get; set; }
    public int JobOfferId { get; set; }
    public int RecruiterUserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
}
