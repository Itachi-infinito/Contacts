using SparkWork2.Repositories;
using SparkWork2.Services;

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

        lblFullName.Text = profile.FullName;
        lblTitle.Text = profile.Title;
        lblLocation.Text = profile.Location;
        lblAbout.Text = profile.About;
        lblEmail.Text = profile.Email;

        if (!string.IsNullOrWhiteSpace(profile.PhotoPath) && File.Exists(profile.PhotoPath))
        {
            imgProfilePhoto.Source = ImageSource.FromFile(profile.PhotoPath);
        }
        else
        {
            imgProfilePhoto.Source = "dotnet_bot.png";
        }
    }

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditCandidateProfilePage));
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}