using SparkWork2.Repositories;
using SparkWork2.Services;

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

        lblCompanyName.Text = profile.CompanyName;
        lblSector.Text = profile.Sector;
        lblLocation.Text = profile.Location;
        lblContactEmail.Text = profile.ContactEmail;
        lblDescription.Text = profile.Description;

        if (!string.IsNullOrWhiteSpace(profile.CompanyPhotoPath) && File.Exists(profile.CompanyPhotoPath))
        {
            imgCompanyPhoto.Source = ImageSource.FromFile(profile.CompanyPhotoPath);
        }
        else
        {
            imgCompanyPhoto.Source = "dotnet_bot.png";
        }
    }

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditRecruiterProfilePage));
    }

    private void Menu_Tapped(object sender, TappedEventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}