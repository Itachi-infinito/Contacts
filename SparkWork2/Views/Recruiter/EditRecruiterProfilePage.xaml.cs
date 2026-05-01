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

        var initials = BuildInitials(_currentProfile.CompanyName);
        lblHeaderInitials.Text = initials;
        lblPhotoInitials.Text = initials;

        SetCompanyPhoto(_currentProfile.CompanyPhotoPath);
    }

    private void SetCompanyPhoto(string? photoPath)
    {
        if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
        {
            imgCompanyPhoto.Source = ImageSource.FromFile(photoPath);
            photoImageFrame.IsVisible = true;
            photoPlaceholderFrame.IsVisible = false;
        }
        else
        {
            imgCompanyPhoto.Source = null;
            photoImageFrame.IsVisible = false;
            photoPlaceholderFrame.IsVisible = true;
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
            await DisplayAlert("Erreur", "Merci de remplir tous les champs.", "OK");
            return;
        }

        if (companyName.Length < 2)
        {
            await DisplayAlert("Erreur", "Le nom de l'entreprise doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (sector.Length < 2)
        {
            await DisplayAlert("Erreur", "Le secteur doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (!IsValidEmail(contactEmail))
        {
            await DisplayAlert("Erreur", "Merci d'entrer une adresse email valide.", "OK");
            return;
        }

        if (description.Length < 10)
        {
            await DisplayAlert("Erreur", "La description doit contenir au moins 10 caractères.", "OK");
            return;
        }

        _currentProfile.CompanyName = companyName;
        _currentProfile.Sector = sector;
        _currentProfile.Location = location;
        _currentProfile.ContactEmail = contactEmail;
        _currentProfile.Description = description;
        _currentProfile.CompanyPhotoPath = _selectedCompanyPhotoPath ?? _currentProfile.CompanyPhotoPath;

        await _recruiterProfileRepository.UpdateRecruiterProfileAsync(_currentProfile);

        await DisplayAlert("Succès", "Profil mis à jour.", "OK");
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
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

    private async void ChangePhoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choisir une photo d'entreprise"
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
            SetCompanyPhoto(localPath);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Impossible de sélectionner la photo : {ex.Message}", "OK");
        }
    }
}
