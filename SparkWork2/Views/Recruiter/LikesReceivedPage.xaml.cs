using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class LikesReceivedPage : ContentPage
{
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly UserRepository _userRepository;
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;

    public LikesReceivedPage(
        CandidateJobLikeRepository candidateJobLikeRepository,
        JobOfferRepository jobOfferRepository,
        UserRepository userRepository,
        MatchRepository matchRepository,
        SessionService sessionService)
    {
        InitializeComponent();
        _candidateJobLikeRepository = candidateJobLikeRepository;
        _jobOfferRepository = jobOfferRepository;
        _userRepository = userRepository;
        _matchRepository = matchRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLikes();
    }

    private async Task LoadLikes()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        int recruiterUserId = _sessionService.CurrentUserId;

        var likes = await _candidateJobLikeRepository.GetLikesForRecruiterOffersAsync(recruiterUserId);
        var allUsers = await _userRepository.GetUsersAsync();
        var recruiterOffers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(recruiterUserId);

        var items = likes.Select(like =>
        {
            var candidate = allUsers.FirstOrDefault(x => x.UserId == like.CandidateUserId);
            var offer = recruiterOffers.FirstOrDefault(x => x.JobOfferId == like.JobOfferId);

            return new RecruiterLikeReceivedItem
            {
                CandidateUserId = like.CandidateUserId,
                CandidateName = candidate?.FullName ?? "Unknown candidate",
                JobOfferId = like.JobOfferId,
                JobTitle = offer?.Title ?? "Unknown offer"
            };
        }).ToList();

        likesCollection.ItemsSource = null;
        likesCollection.ItemsSource = items;

        emptyStateFrame.IsVisible = items.Count == 0;
    }

    private async void Candidate_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is RecruiterLikeReceivedItem selectedCandidate)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(CandidateDetailPage)}?candidateId={selectedCandidate.CandidateUserId}");
        }
    }

    private async void DeleteBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not RecruiterLikeReceivedItem selectedLike)
            return;

        var popup = new DeleteConfirmationPopup("Remove this match?");
        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;
        if (!confirmed)
            return;

        await _candidateJobLikeRepository.DeleteLikeAsync(
            selectedLike.CandidateUserId,
            selectedLike.JobOfferId);

        await LoadLikes();
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}