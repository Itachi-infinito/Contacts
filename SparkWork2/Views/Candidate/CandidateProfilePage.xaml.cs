using SparkWork2.Models;
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

        lblFullName.Text = fullName;
        lblTitle.Text = string.IsNullOrWhiteSpace(profile.Title) ? "Titre non renseigné" : profile.Title;
        lblLocation.Text = string.IsNullOrWhiteSpace(profile.Location) ? "Localisation non renseignée" : profile.Location;
        lblAbout.Text = string.IsNullOrWhiteSpace(profile.About)
            ? "Ajoute une courte description pour aider les recruteurs à mieux comprendre ton profil."
            : profile.About;
        lblEmail.Text = string.IsNullOrWhiteSpace(profile.Email) ? _sessionService.CurrentUserEmail : profile.Email;

        lblDesiredContractType.Text = string.IsNullOrWhiteSpace(profile.DesiredContractType)
            ? "Non renseigné"
            : profile.DesiredContractType;

        lblExperienceLevel.Text = string.IsNullOrWhiteSpace(profile.ExperienceLevel)
            ? "Non renseigné"
            : profile.ExperienceLevel;

        lblDesiredSalary.Text = GetSalaryDisplay(profile);
        lblMaxDistance.Text = profile.MaxDistanceKm > 0
            ? $"{profile.MaxDistanceKm} km"
            : "Non renseignée";

        var initials = BuildInitials(fullName);
        lblHeaderInitials.Text = initials;
        lblProfileInitials.Text = initials;

        SetProfilePhoto(profile.PhotoPath);
        RenderSkills(profile.Skills);
    }

    private void SetProfilePhoto(string? photoPath)
    {
        if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
        {
            imgProfilePhoto.Source = ImageSource.FromFile(photoPath);
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

    private void RenderSkills(string? skillsText)
    {
        skillsLayout.Children.Clear();

        var skills = (skillsText ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Take(8)
            .ToList();

        lblNoSkills.IsVisible = skills.Count == 0;

        foreach (var skill in skills)
        {
            var chip = new Frame
            {
                BackgroundColor = Color.FromArgb("#F0EAFE"),
                CornerRadius = 12,
                Padding = new Thickness(10, 4),
                HasShadow = false,
                Margin = new Thickness(0, 0, 8, 8),
                Content = new Label
                {
                    Text = skill,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#7C4DFF")
                }
            };

            skillsLayout.Children.Add(chip);
        }
    }

    private static string GetSalaryDisplay(CandidateProfile profile)
    {
        if (profile.DesiredSalaryMin > 0 && profile.DesiredSalaryMax > 0)
            return $"{profile.DesiredSalaryMin} - {profile.DesiredSalaryMax} €";

        if (profile.DesiredSalaryMin > 0)
            return $"Dès {profile.DesiredSalaryMin} €";

        if (profile.DesiredSalaryMax > 0)
            return $"Jusqu'à {profile.DesiredSalaryMax} €";

        return "Non renseigné";
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

    private async void Home_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateHomePage)}");
    }

    private async void Discover_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateSwipePage)}");
    }

    private async void Messages_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditCandidateProfilePage));
    }

    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
