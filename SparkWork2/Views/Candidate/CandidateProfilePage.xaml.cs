using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Candidate;

public partial class CandidateProfilePage : ContentPage
{
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly SessionService _sessionService;

    public CandidateProfilePage(
        CandidateProfileRepository candidateProfileRepository,
        SessionService sessionService)
    {
        InitializeComponent();
        _candidateProfileRepository = candidateProfileRepository;
        _sessionService = sessionService;
    }
    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
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

        var profile = await _candidateProfileRepository.GetCandidateProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        var fullName = string.IsNullOrWhiteSpace(profile.FullName)
            ? _sessionService.CurrentUserName
            : profile.FullName;

        var title = string.IsNullOrWhiteSpace(profile.Title)
            ? "Titre non renseigné"
            : profile.Title;

        var location = string.IsNullOrWhiteSpace(profile.Location)
            ? "Localisation non renseignée"
            : profile.Location;

        var about = string.IsNullOrWhiteSpace(profile.About)
            ? "Ajoute une courte description pour aider les recruteurs à mieux comprendre ton profil."
            : profile.About;

        var email = string.IsNullOrWhiteSpace(profile.Email)
            ? _sessionService.CurrentUserEmail
            : profile.Email;

        lblFullName.Text = fullName;
        lblTitle.Text = title;
        lblLocation.Text = location;
        lblAbout.Text = about;
        lblEmail.Text = email;

        var initials = BuildInitials(fullName);
        lblHeaderInitials.Text = initials;
        lblProfileInitials.Text = initials;

        if (!string.IsNullOrWhiteSpace(profile.PhotoPath) && File.Exists(profile.PhotoPath))
        {
            imgProfilePhoto.Source = ImageSource.FromFile(profile.PhotoPath);
            profilePhotoFrame.IsVisible = true;
            profilePlaceholderFrame.IsVisible = false;
        }
        else
        {
            imgProfilePhoto.Source = null;
            profilePhotoFrame.IsVisible = false;
            profilePlaceholderFrame.IsVisible = true;
        }
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "ME";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditCandidateProfilePage));
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
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
}
