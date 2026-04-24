using System.Text.RegularExpressions;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;

namespace SparkWork2.Views.Candidate;

public partial class EditCandidateProfilePage : ContentPage
{
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly SessionService _sessionService;

    private string? _selectedPhotoPath;
    private CandidateProfile? _currentProfile;

    public EditCandidateProfilePage(
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

        _currentProfile = await _candidateProfileRepository.GetCandidateProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        entryFullName.Text = _currentProfile.FullName;
        entryTitle.Text = _currentProfile.Title;
        entryLocation.Text = _currentProfile.Location;
        entryEmail.Text = _currentProfile.Email;
        editorAbout.Text = _currentProfile.About;

        if (!string.IsNullOrWhiteSpace(_currentProfile.PhotoPath) && File.Exists(_currentProfile.PhotoPath))
        {
            imgCandidatePhoto.Source = ImageSource.FromFile(_currentProfile.PhotoPath);
        }
        else
        {
            imgCandidatePhoto.Source = "dotnet_bot.png";
        }
    }

    private async void Update_Clicked(object sender, EventArgs e)
    {
        if (_currentProfile == null)
            return;

        string fullName = entryFullName.Text?.Trim() ?? string.Empty;
        string title = entryTitle.Text?.Trim() ?? string.Empty;
        string location = entryLocation.Text?.Trim() ?? string.Empty;
        string email = entryEmail.Text?.Trim() ?? string.Empty;
        string about = editorAbout.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(location) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(about))
        {
            await DisplayAlert("Error", "Please fill in all fields.", "OK");
            return;
        }

        if (fullName.Length < 2)
        {
            await DisplayAlert("Error", "Full name must contain at least 2 characters.", "OK");
            return;
        }

        if (title.Length < 2)
        {
            await DisplayAlert("Error", "Title must contain at least 2 characters.", "OK");
            return;
        }

        if (!IsValidEmail(email))
        {
            await DisplayAlert("Error", "Please enter a valid email address.", "OK");
            return;
        }

        if (about.Length < 10)
        {
            await DisplayAlert("Error", "About must contain at least 10 characters.", "OK");
            return;
        }

        _currentProfile.FullName = fullName;
        _currentProfile.Title = title;
        _currentProfile.Location = location;
        _currentProfile.Email = email;
        _currentProfile.About = about;
        _currentProfile.PhotoPath = _selectedPhotoPath ?? _currentProfile.PhotoPath;

        await _candidateProfileRepository.UpdateCandidateProfileAsync(_currentProfile);

        await DisplayAlert("Success", "Profile updated successfully.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private async void ChangePhoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choose your profile photo"
            });

            if (result == null)
                return;

            string extension = Path.GetExtension(result.FileName);
            string fileName = $"candidate_{Guid.NewGuid()}{extension}";
            string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            await using var sourceStream = await result.OpenReadAsync();
            await using var localFileStream = File.OpenWrite(localPath);
            await sourceStream.CopyToAsync(localFileStream);

            _selectedPhotoPath = localPath;
            imgCandidatePhoto.Source = ImageSource.FromFile(localPath);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to select photo: {ex.Message}", "OK");
        }
    }
}