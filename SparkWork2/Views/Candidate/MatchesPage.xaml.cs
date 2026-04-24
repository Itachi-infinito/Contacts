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
        int candidateUserId = _sessionService.CurrentUserId;

        var matches = await _matchRepository.GetMatchesAsync(candidateUserId);
        var visibleMatches = new List<Match>();

        foreach (var match in matches)
        {
            var offer = await _jobOfferRepository.GetJobOfferByIdAsync(match.JobOfferId);
            if (offer == null)
                continue;

            visibleMatches.Add(new Match
            {
                MatchId = match.MatchId,
                UserId = match.UserId,
                CandidateUserId = match.CandidateUserId,
                CandidateName = match.CandidateName,
                RecruiterUserId = match.RecruiterUserId,
                JobOfferId = match.JobOfferId,
                JobTitle = offer.Title,
                CompanyName = offer.CompanyName,
                ShowToCandidate = match.ShowToCandidate,
                ShowToRecruiter = match.ShowToRecruiter
            });
        }

        matchesCollection.ItemsSource = null;
        matchesCollection.ItemsSource = visibleMatches;

        noMatchesLabel.IsVisible = visibleMatches.Count == 0;
    }

    private async void Match_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Match selectedMatch)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(JobOfferDetailPage)}?id={selectedMatch.JobOfferId}");
        }
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void OnViewDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Match match)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(JobOfferDetailPage)}?id={match.JobOfferId}");
        }
    }

    private async void DeleteBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not Match match)
            return;

        await BubbleTapAnimation(frame);

        var popup = new DeleteConfirmationPopup("Remove this match?");
        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;
        if (!confirmed)
            return;

        await _matchRepository.RemoveMatchAsync(match.MatchId);
        await LoadMatches();
    }

    private async void SwipeBubble_Loaded(object sender, EventArgs e)
    {
        if (sender is not Frame bubble)
            return;

        if (Math.Abs(bubble.Scale - 1) < 0.01)
            return;

        await bubble.ScaleTo(1, 220, Easing.SpringOut);
    }

    private async void MatchCard_Loaded(object sender, EventArgs e)
    {
        if (sender is not Frame card)
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

    private async Task BubbleTapAnimation(Frame bubble)
    {
        await bubble.ScaleTo(0.88, 70, Easing.CubicIn);
        await bubble.ScaleTo(1.0, 140, Easing.SpringOut);
    }
}