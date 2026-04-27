using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;


namespace SparkWork2.Views.Candidate;

[QueryProperty(nameof(JobOfferId), "id")]
public partial class JobOfferDetailPage : ContentPage
{
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;
    private readonly RecruiterCandidateLikeRepository _recruiterCandidateLikeRepository;
    private readonly MatchRepository _matchRepository;


    private JobOffer? _currentJobOffer;

    public JobOfferDetailPage(
    JobOfferRepository jobOfferRepository,
    CandidateJobLikeRepository candidateJobLikeRepository,
    RecruiterCandidateLikeRepository recruiterCandidateLikeRepository,
    MatchRepository matchRepository,
    SessionService sessionService)
    {
        InitializeComponent();

        _jobOfferRepository = jobOfferRepository;
        _candidateJobLikeRepository = candidateJobLikeRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _matchRepository = matchRepository;
        _sessionService = sessionService;
    }


    public string JobOfferId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                _ = LoadJobOffer(id);
            }
        }
    }

    private async Task LoadJobOffer(int id)
    {
        _currentJobOffer = await _jobOfferRepository.GetJobOfferByIdAsync(id);

        if (_currentJobOffer == null)
            return;

        lblTitle.Text = _currentJobOffer.Title;
        lblCompany.Text = _currentJobOffer.CompanyName;
        lblLocation.Text = _currentJobOffer.Location;
        lblContractType.Text = _currentJobOffer.ContractType;
        lblDescription.Text = _currentJobOffer.Description;

        await UpdateLikeButton();
    }

    private async void Like_Clicked(object sender, EventArgs e)
    {
        if (_currentJobOffer == null || !_sessionService.IsLoggedIn)
            return;

        bool added = await _candidateJobLikeRepository.AddLikeAsync(
            _sessionService.CurrentUserId,
            _currentJobOffer.JobOfferId);

        if (!added)
        {
            await DisplayAlert("Info", "You already liked this offer.", "OK");
            return;
        }

        await UpdateLikeButton();

        bool recruiterLikedCandidate =
            await _recruiterCandidateLikeRepository.HasRecruiterLikedCandidateAsync(
                _currentJobOffer.RecruiterUserId,
                _sessionService.CurrentUserId);

        if (recruiterLikedCandidate)
        {
            await _matchRepository.AddMatchAsync(
                _sessionService.CurrentUserId,
                _sessionService.CurrentUserName,
                _currentJobOffer.RecruiterUserId,
                _currentJobOffer,
                true);

            await Shell.Current.GoToAsync(
                $"{nameof(MatchPage)}" +
                $"?participantId={_currentJobOffer.RecruiterUserId}" +
                $"&participantName={Uri.EscapeDataString(_currentJobOffer.CompanyName)}");

            return;
        }

        await DisplayAlert("Like sent", "Your interest has been sent to the recruiter.", "OK");
    }


    private async Task UpdateLikeButton()
    {
        if (_currentJobOffer == null || !_sessionService.IsLoggedIn)
            return;

        bool alreadyLiked = await _candidateJobLikeRepository.IsLikedAsync(
            _sessionService.CurrentUserId,
            _currentJobOffer.JobOfferId);

        if (alreadyLiked)
        {
            btnLike.Text = "Already liked";
            btnLike.IsEnabled = false;
            btnLike.BackgroundColor = Colors.White;
            btnLike.BorderColor = Color.FromArgb("#CDEDE7");
            btnLike.TextColor = Color.FromArgb("#7B8F8B");
        }
        else
        {
            btnLike.Text = "Like";
            btnLike.IsEnabled = true;
            btnLike.BackgroundColor = Colors.White;
            btnLike.BorderColor = Color.FromArgb("#BFEADF");
            btnLike.TextColor = Color.FromArgb("#1ABC9C");
        }
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}