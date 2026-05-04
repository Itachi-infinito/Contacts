using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class EditRecruiterProfilePage : ContentPage

{
    private readonly RecruiterProfileRepository _recruiterProfileRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;

    public EditRecruiterProfilePage(

        RecruiterProfileRepository recruiterProfileRepository,
        JobOfferRepository jobOfferRepository,
        MatchRepository matchRepository,
        SessionService sessionService)
    {
        InitializeComponent();

        _recruiterProfileRepository = recruiterProfileRepository;
        _jobOfferRepository = jobOfferRepository;
        _matchRepository = matchRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfile();
    }

    private async Task LoadProfile()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var profile = await _recruiterProfileRepository.GetRecruiterProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        var companyName = string.IsNullOrWhiteSpace(profile.CompanyName)
            ? _sessionService.CurrentUserName
            : profile.CompanyName;

        lblCompanyName.Text = companyName;
        lblSector.Text = string.IsNullOrWhiteSpace(profile.Sector) ? "Secteur non renseigné" : profile.Sector;
        lblLocation.Text = string.IsNullOrWhiteSpace(profile.Location) ? "Localisation non renseignée" : profile.Location;
        lblDescription.Text = string.IsNullOrWhiteSpace(profile.Description)
            ? "Ajoutez une description pour présenter votre entreprise aux candidats."
            : profile.Description;
        lblContactEmail.Text = string.IsNullOrWhiteSpace(profile.ContactEmail)
            ? _sessionService.CurrentUserEmail
            : profile.ContactEmail;

        var initials = BuildInitials(companyName);
        lblHeaderInitials.Text = initials;
        lblCompanyInitials.Text = initials;

        SetCompanyPhoto(profile.CompanyPhotoPath);

        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);
        lblOffersCount.Text = offers.Count.ToString();

        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);
        lblMatchesCount.Text = matches.Count.ToString();
    }

    private void SetCompanyPhoto(string? photoPath)
    {
        if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
        {
            imgCompanyPhoto.Source = ImageSource.FromFile(photoPath);
            companyPhotoFrame.IsVisible = true;
            companyPlaceholderFrame.IsVisible = false;
        }
        else
        {
            imgCompanyPhoto.Source = null;
            companyPhotoFrame.IsVisible = false;
            companyPlaceholderFrame.IsVisible = true;
        }
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "AL";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async void Home_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterHomePage)}");
    }

    private async void Discover_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private async void AddOffer_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));
    }

    private async void Messages_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditRecruiterProfilePage));
    }

    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
