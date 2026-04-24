using System.Text.RegularExpressions;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;

namespace SparkWork2.Views.Recruiter;

public partial class EditRecruiterProfilePage : ContentPage
{
    private readonly RecruiterProfileRepository _recruiterProfileRepository;
    private readonly SessionService _sessionService;

    private string? _selectedCompanyPhotoPath;
    private RecruiterProfile? _currentProfile;

    public EditRecruiterProfilePage(
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

        _currentProfile = await _recruiterProfileRepository.GetRecruiterProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        entryCompanyName.Text = _currentProfile.CompanyName;
        entrySector.Text = _currentProfile.Sector;
        entryLocation.Text = _currentProfile.Location;
        entryContactEmail.Text = _currentProfile.ContactEmail;
        editorDescription.Text = _currentProfile.Description;

        if (!string.IsNullOrWhiteSpace(_currentProfile.CompanyPhotoPath) && File.Exists(_currentProfile.CompanyPhotoPath))
        {
            imgCompanyPhoto.Source = ImageSource.FromFile(_currentProfile.CompanyPhotoPath);
        }
        else
        {
            imgCompanyPhoto.Source = "dotnet_bot.png";
        }
    }

    private async void Update_Clicked(object sender, EventArgs e)
    {
        if (_currentProfile == null)
            return;

        string companyName = entryCompanyName.Text?.Trim() ?? string.Empty;
        string sector = entrySector.Text?.Trim() ?? string.Empty;
        string location = entryLocation.Text?.Trim() ?? string.Empty;
        string contactEmail = entryContactEmail.Text?.Trim() ?? string.Empty;
        string description = editorDescription.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(companyName) ||
            string.IsNullOrWhiteSpace(sector) ||
            string.IsNullOrWhiteSpace(location) ||
            string.IsNullOrWhiteSpace(contactEmail) ||
            string.IsNullOrWhiteSpace(description))
        {
            await DisplayAlert("Error", "Please fill in all fields.", "OK");
            return;
        }

        if (companyName.Length < 2)
        {
            await DisplayAlert("Error", "Company name must contain at least 2 characters.", "OK");
            return;
        }

        if (sector.Length < 2)
        {
            await DisplayAlert("Error", "Sector must contain at least 2 characters.", "OK");
            return;
        }

        if (!IsValidEmail(contactEmail))
        {
            await DisplayAlert("Error", "Please enter a valid contact email.", "OK");
            return;
        }

        if (description.Length < 10)
        {
            await DisplayAlert("Error", "Description must contain at least 10 characters.", "OK");
            return;
        }

        _currentProfile.CompanyName = companyName;
        _currentProfile.Sector = sector;
        _currentProfile.Location = location;
        _currentProfile.ContactEmail = contactEmail;
        _currentProfile.Description = description;
        _currentProfile.CompanyPhotoPath = _selectedCompanyPhotoPath ?? _currentProfile.CompanyPhotoPath;

        await _recruiterProfileRepository.UpdateRecruiterProfileAsync(_currentProfile);

        await DisplayAlert("Success", "Profile updated successfully.", "OK");
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
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
                Title = "Choose company photo"
            });

            if (result == null)
                return;

            string extension = Path.GetExtension(result.FileName);
            string fileName = $"company_{Guid.NewGuid()}{extension}";
            string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            await using var sourceStream = await result.OpenReadAsync();
            await using var localFileStream = File.OpenWrite(localPath);
            await sourceStream.CopyToAsync(localFileStream);

            _selectedCompanyPhotoPath = localPath;
            imgCompanyPhoto.Source = ImageSource.FromFile(localPath);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to select photo: {ex.Message}", "OK");
        }
    }
}