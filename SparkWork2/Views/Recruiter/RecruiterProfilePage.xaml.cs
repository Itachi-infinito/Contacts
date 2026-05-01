using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterProfilePage : ContentPage
{
    private readonly RecruiterProfileRepository _recruiterProfileRepository;
    private readonly SessionService _sessionService;

    public RecruiterProfilePage(
        RecruiterProfileRepository recruiterProfileRepository,
        SessionService sessionService)
    {
        InitializeComponent();
        _recruiterProfileRepository = recruiterProfileRepository;
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

        var sector = string.IsNullOrWhiteSpace(profile.Sector)
            ? "Secteur non renseigné"
            : profile.Sector;

        var location = string.IsNullOrWhiteSpace(profile.Location)
            ? "Localisation non renseignée"
            : profile.Location;

        var contactEmail = string.IsNullOrWhiteSpace(profile.ContactEmail)
            ? _sessionService.CurrentUserEmail
            : profile.ContactEmail;

        var description = string.IsNullOrWhiteSpace(profile.Description)
            ? "Ajoute une description pour présenter ton entreprise aux candidats."
            : profile.Description;

        lblCompanyName.Text = companyName;
        lblSector.Text = sector;
        lblLocation.Text = location;
        lblContactEmail.Text = contactEmail;
        lblDescription.Text = description;

        var initials = BuildInitials(companyName);
        lblHeaderInitials.Text = initials;
        lblCompanyInitials.Text = initials;

        if (!string.IsNullOrWhiteSpace(profile.CompanyPhotoPath) && File.Exists(profile.CompanyPhotoPath))
        {
            imgCompanyPhoto.Source = ImageSource.FromFile(profile.CompanyPhotoPath);
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

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditRecruiterProfilePage));
    }
    private async void Offers_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterJobOffersPage));
    }


    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterMatchesPage)}");
    }
    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

}
