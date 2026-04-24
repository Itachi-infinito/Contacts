using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterMatchesPage : ContentPage
{
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly RecruiterCandidateLikeRepository _recruiterCandidateLikeRepository;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly UserRepository _userRepository;
    private readonly SessionService _sessionService;

    public RecruiterMatchesPage(
        CandidateJobLikeRepository candidateJobLikeRepository,
        RecruiterCandidateLikeRepository recruiterCandidateLikeRepository,
        CandidateProfileRepository candidateProfileRepository,
        UserRepository userRepository,
        SessionService sessionService)
    {
        InitializeComponent();
        _candidateJobLikeRepository = candidateJobLikeRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _candidateProfileRepository = candidateProfileRepository;
        _userRepository = userRepository;
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

        int recruiterUserId = _sessionService.CurrentUserId;

        var recruiterLikes = await _recruiterCandidateLikeRepository.GetLikesByRecruiterAsync(recruiterUserId);
        var allProfiles = await _candidateProfileRepository.GetAllCandidateProfilesAsync();

        var mutualMatches = new List<CandidateBrowseItem>();

        foreach (var like in recruiterLikes)
        {
            bool candidateLikedRecruiter =
                await _candidateJobLikeRepository.HasCandidateLikedAnyOfferOfRecruiterAsync(
                    like.CandidateUserId,
                    recruiterUserId);

            if (!candidateLikedRecruiter)
                continue;

            var user = await _userRepository.GetUserByIdAsync(like.CandidateUserId);
            if (user == null)
                continue;

            var profile = allProfiles.FirstOrDefault(x => x.CandidateId == like.CandidateUserId);
            if (profile == null)
                continue;

            mutualMatches.Add(new CandidateBrowseItem
            {
                CandidateUserId = profile.CandidateId,
                FullName = profile.FullName,
                Title = profile.Title,
                Location = profile.Location,
                About = profile.About,
                Email = profile.Email
            });
        }

        matchesCollection.ItemsSource = null;
        matchesCollection.ItemsSource = mutualMatches;

        emptyStateFrame.IsVisible = mutualMatches.Count == 0;
    }

    private async void Candidate_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is CandidateBrowseItem selectedCandidate)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(CandidateDetailPage)}?candidateId={selectedCandidate.CandidateUserId}");
        }
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async Task DeleteMatchAsync(int candidateUserId)
    {
        if (!_sessionService.IsLoggedIn)
            return;

        int recruiterUserId = _sessionService.CurrentUserId;

        await _recruiterCandidateLikeRepository.DeleteLikeAsync(recruiterUserId, candidateUserId);
        await _candidateJobLikeRepository.DeleteLikesForCandidateAndRecruiterAsync(candidateUserId, recruiterUserId);
    }

    private async void DeleteBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not CandidateBrowseItem selectedCandidate)
            return;

        await BubbleTapAnimation(frame);

        var popup = new DeleteConfirmationPopup("Remove this match?");
        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;
        if (!confirmed)
            return;

        await DeleteMatchAsync(selectedCandidate.CandidateUserId);
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