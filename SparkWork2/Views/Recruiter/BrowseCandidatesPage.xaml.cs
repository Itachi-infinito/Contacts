using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;


namespace SparkWork2.Views.Recruiter;

public partial class BrowseCandidatesPage : ContentPage
{
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly RecruiterCandidateLikeRepository _recruiterCandidateLikeRepository;
    private readonly SessionService _sessionService;
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly MatchRepository _matchRepository;


    public BrowseCandidatesPage(
    CandidateProfileRepository candidateProfileRepository,
    RecruiterCandidateLikeRepository recruiterCandidateLikeRepository,
    CandidateJobLikeRepository candidateJobLikeRepository,
    MatchRepository matchRepository,
    SessionService sessionService)
    {
        InitializeComponent();

        _candidateProfileRepository = candidateProfileRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _candidateJobLikeRepository = candidateJobLikeRepository;
        _matchRepository = matchRepository;
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

        int recruiterUserId = _sessionService.CurrentUserId;

        var profiles = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        var recruiterLikes = await _recruiterCandidateLikeRepository.GetLikesByRecruiterAsync(recruiterUserId);

        var candidates = profiles
            .Where(x => x.CandidateId != recruiterUserId)
            .Select(x => new CandidateBrowseItem
            {
                CandidateUserId = x.CandidateId,
                FullName = x.FullName,
                Title = x.Title,
                Location = x.Location,
                About = x.About,
                Email = x.Email,
                IsAlreadyLiked = recruiterLikes.Any(like => like.CandidateUserId == x.CandidateId)
            })
            .ToList();

        candidatesCollection.ItemsSource = null;
        candidatesCollection.ItemsSource = candidates;

        noCandidatesLabel.IsVisible = candidates.Count == 0;
    }

    private async void LikeCandidate_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not CandidateBrowseItem selectedCandidate)
            return;

        bool added = await _recruiterCandidateLikeRepository.AddLikeAsync(
            _sessionService.CurrentUserId,
            selectedCandidate.CandidateUserId);

        if (!added)
        {
            await DisplayAlert("Info", "You already liked this candidate.", "OK");
            return;
        }

        var matchedOffer = await _candidateJobLikeRepository.GetFirstLikedOfferOfRecruiterAsync(
            selectedCandidate.CandidateUserId,
            _sessionService.CurrentUserId);

        if (matchedOffer != null)
        {
            await _matchRepository.AddMatchAsync(
                selectedCandidate.CandidateUserId,
                selectedCandidate.FullName,
                _sessionService.CurrentUserId,
                matchedOffer,
                false);

            await Shell.Current.GoToAsync(
                $"{nameof(MatchPage)}" +
                $"?participantId={selectedCandidate.CandidateUserId}" +
                $"&participantName={Uri.EscapeDataString(selectedCandidate.FullName)}");

            return;
        }

        await DisplayAlert("Success", $"{selectedCandidate.FullName} added to your likes.", "OK");
        await LoadCandidates();
    }


    private async void Candidate_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is CandidateBrowseItem selectedCandidate)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(CandidateDetailPage)}?candidateId={selectedCandidate.CandidateUserId}");
        }
    }
}