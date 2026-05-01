using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;
using Microsoft.Maui.ApplicationModel;


namespace SparkWork2.Views.Candidate;

[QueryProperty(nameof(JobOfferId), "id")]
public partial class JobOfferDetailPage : ContentPage
{
    private readonly CandidateJobLikeRepository _candidateJobLikeRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;
    private readonly RecruiterCandidateLikeRepository _recruiterCandidateLikeRepository;
    private readonly MatchRepository _matchRepository;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly CompatibilityService _compatibilityService;


    private JobOffer? _currentJobOffer;

    public JobOfferDetailPage(
        JobOfferRepository jobOfferRepository,
        CandidateJobLikeRepository candidateJobLikeRepository,
        RecruiterCandidateLikeRepository recruiterCandidateLikeRepository,
        MatchRepository matchRepository,
        CandidateProfileRepository candidateProfileRepository,
        CompatibilityService compatibilityService,
        SessionService sessionService)

    {
        InitializeComponent();

        _jobOfferRepository = jobOfferRepository;
        _candidateJobLikeRepository = candidateJobLikeRepository;
        _recruiterCandidateLikeRepository = recruiterCandidateLikeRepository;
        _matchRepository = matchRepository;
        _sessionService = sessionService;
        _candidateProfileRepository = candidateProfileRepository;
        _compatibilityService = compatibilityService;

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
        {
            await DisplayAlert("Erreur", "Offre introuvable.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        lblTitle.Text = _currentJobOffer.Title;
        lblCompany.Text = _currentJobOffer.CompanyName;
        lblLocation.Text = _currentJobOffer.Location;
        btnOpenMaps.IsVisible =
    !string.IsNullOrWhiteSpace(_currentJobOffer.Address) ||
    !string.IsNullOrWhiteSpace(_currentJobOffer.Location);

        lblContractType.Text = _currentJobOffer.ContractType;
        lblDescription.Text = _currentJobOffer.Description;
        levelBadge.IsVisible = !string.IsNullOrWhiteSpace(_currentJobOffer.Level);
        lblLevel.Text = _currentJobOffer.Level;


        remoteBadge.IsVisible = !string.IsNullOrWhiteSpace(_currentJobOffer.RemoteMode);
        lblRemoteMode.Text = _currentJobOffer.RemoteMode;

        salaryCard.IsVisible = _currentJobOffer.SalaryMin > 0 || _currentJobOffer.SalaryMax > 0;
        lblSalary.Text = GetSalaryDisplay(_currentJobOffer);

        requiredSkillsLayout.IsVisible = !string.IsNullOrWhiteSpace(_currentJobOffer.RequiredSkills);
        lblRequiredSkills.Text = _currentJobOffer.RequiredSkills;

        niceSkillsLayout.IsVisible = !string.IsNullOrWhiteSpace(_currentJobOffer.NiceToHaveSkills);
        lblNiceToHaveSkills.Text = _currentJobOffer.NiceToHaveSkills;

        skillsCard.IsVisible = requiredSkillsLayout.IsVisible || niceSkillsLayout.IsVisible;

        await UpdateCompatibilityDetails();

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
            await DisplayAlert("Info", "Tu as déjà liké cette offre.", "OK");
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

        await DisplayAlert("Intérêt envoyé", "Ton intérêt a été envoyé au recruteur.", "OK");
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
            btnLike.Text = "Offre déjà likée";
            btnLike.IsEnabled = false;
            btnLike.BackgroundColor = Colors.White;
            btnLike.BorderColor = Color.FromArgb("#DED8F5");
            btnLike.BorderWidth = 1;
            btnLike.TextColor = Color.FromArgb("#8581A6");
        }
        else
        {
            btnLike.Text = "Liker cette offre";
            btnLike.IsEnabled = true;
            btnLike.BackgroundColor = Color.FromArgb("#7C4DFF");
            btnLike.BorderColor = Colors.Transparent;
            btnLike.BorderWidth = 0;
            btnLike.TextColor = Colors.White;
        }
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateSwipePage)}");
    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MatchesPage)}");
    }
    private string GetSalaryDisplay(JobOffer jobOffer)
    {
        if (jobOffer.SalaryMin > 0 && jobOffer.SalaryMax > 0)
            return $"{jobOffer.SalaryMin} - {jobOffer.SalaryMax} €";

        if (jobOffer.SalaryMin > 0)
            return $"À partir de {jobOffer.SalaryMin} €";

        if (jobOffer.SalaryMax > 0)
            return $"Jusqu'à {jobOffer.SalaryMax} €";

        return "Salaire non renseigné";
    }
    private async Task UpdateCompatibilityDetails()
    {
        if (_currentJobOffer == null || !_sessionService.IsLoggedIn)
        {
            compatibilityCard.IsVisible = false;
            return;
        }

        var candidateProfile = await _candidateProfileRepository.GetCandidateProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        int score = _compatibilityService.CalculateScore(candidateProfile, _currentJobOffer);

        compatibilityCard.IsVisible = true;
        lblCompatibilityScore.Text = $"{score}%";
        compatibilityProgress.Progress = score / 100.0;

        if (score >= 75)
        {
            lblCompatibilityScore.TextColor = Color.FromArgb("#10B981");
            compatibilityProgress.ProgressColor = Color.FromArgb("#10B981");
        }
        else if (score >= 45)
        {
            lblCompatibilityScore.TextColor = Color.FromArgb("#7C4DFF");
            compatibilityProgress.ProgressColor = Color.FromArgb("#7C4DFF");
        }
        else
        {
            lblCompatibilityScore.TextColor = Color.FromArgb("#E11D48");
            compatibilityProgress.ProgressColor = Color.FromArgb("#E11D48");
        }

        var matchedRequired = _compatibilityService.GetMatchedRequiredSkills(candidateProfile, _currentJobOffer);
        matchedRequiredSkillsSection.IsVisible = matchedRequired.Any();
        lblMatchedRequiredSkills.Text = string.Join(", ", matchedRequired);

        var missingRequired = _compatibilityService.GetMissingRequiredSkills(candidateProfile, _currentJobOffer);
        missingRequiredSkillsSection.IsVisible = missingRequired.Any();
        lblMissingRequiredSkills.Text = string.Join(", ", missingRequired);

        var matchedNice = _compatibilityService.GetMatchedNiceToHaveSkills(candidateProfile, _currentJobOffer);
        matchedNiceSkillsSection.IsVisible = matchedNice.Any();
        lblMatchedNiceSkills.Text = string.Join(", ", matchedNice);
    }
    private async void OpenMaps_Clicked(object sender, EventArgs e)
    {
        if (_currentJobOffer == null)
            return;

        string destination = !string.IsNullOrWhiteSpace(_currentJobOffer.Address)
            ? _currentJobOffer.Address
            : _currentJobOffer.Location;

        if (string.IsNullOrWhiteSpace(destination))
            return;

        string encodedDestination = Uri.EscapeDataString(destination);
        string url = $"https://www.google.com/maps/dir/?api=1&destination={encodedDestination}";

        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch
        {
            await DisplayAlert("Erreur", "Impossible d'ouvrir l'itinéraire.", "OK");
        }
    }


}
