using System.Text.RegularExpressions;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;


namespace SparkWork2.Views.Candidate;

public partial class EditCandidateProfilePage : ContentPage
{
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly SessionService _sessionService;

    private string? _selectedPhotoPath;
    private CandidateProfile? _currentProfile;
    private double _selectedLatitude;
    private double _selectedLongitude;



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
        entrySkills.Text = _currentProfile.Skills;
        entryDesiredContractType.Text = _currentProfile.DesiredContractType;
        entryExperienceLevel.Text = _currentProfile.ExperienceLevel;
        entryDesiredSalaryMin.Text = _currentProfile.DesiredSalaryMin > 0 ? _currentProfile.DesiredSalaryMin.ToString() : string.Empty;
        entryDesiredSalaryMax.Text = _currentProfile.DesiredSalaryMax > 0 ? _currentProfile.DesiredSalaryMax.ToString() : string.Empty;
        entryMaxDistanceKm.Text = _currentProfile.MaxDistanceKm > 0 ? _currentProfile.MaxDistanceKm.ToString() : "25";

        _selectedLatitude = _currentProfile.Latitude;
        _selectedLongitude = _currentProfile.Longitude;

        UpdateCoordinatesLabel();



        var initials = BuildInitials(_currentProfile.FullName);
        lblHeaderInitials.Text = initials;
        lblPhotoInitials.Text = initials;

        SetProfilePhoto(_currentProfile.PhotoPath);
    }

    private void SetProfilePhoto(string? photoPath)
    {
        if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
        {
            imgCandidatePhoto.Source = ImageSource.FromFile(photoPath);
            photoImageFrame.IsVisible = true;
            photoPlaceholderFrame.IsVisible = false;
        }
        else
        {
            imgCandidatePhoto.Source = null;
            photoImageFrame.IsVisible = false;
            photoPlaceholderFrame.IsVisible = true;
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
        string skills = entrySkills.Text?.Trim() ?? string.Empty;
        string desiredContractType = entryDesiredContractType.Text?.Trim() ?? string.Empty;
        string experienceLevel = entryExperienceLevel.Text?.Trim() ?? string.Empty;

        int.TryParse(entryDesiredSalaryMin.Text?.Trim(), out int desiredSalaryMin);
        int.TryParse(entryDesiredSalaryMax.Text?.Trim(), out int desiredSalaryMax);
        int.TryParse(entryMaxDistanceKm.Text?.Trim(), out int maxDistanceKm);


        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(location) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(about))
        {
            await DisplayAlert("Erreur", "Merci de remplir tous les champs.", "OK");
            return;
        }

        if (fullName.Length < 2)
        {
            await DisplayAlert("Erreur", "Le nom doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (title.Length < 2)
        {
            await DisplayAlert("Erreur", "Le titre doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (!IsValidEmail(email))
        {
            await DisplayAlert("Erreur", "Merci d'entrer une adresse email valide.", "OK");
            return;
        }

        if (about.Length < 10)
        {
            await DisplayAlert("Erreur", "La description doit contenir au moins 10 caractères.", "OK");
            return;
        }
        if (desiredSalaryMin < 0 || desiredSalaryMax < 0)
        {
            await DisplayAlert("Erreur", "Le salaire ne peut pas être négatif.", "OK");
            return;
        }

        if (desiredSalaryMin > 0 && desiredSalaryMax > 0 && desiredSalaryMin > desiredSalaryMax)
        {
            await DisplayAlert("Erreur", "Le salaire minimum ne peut pas être supérieur au salaire maximum.", "OK");
            return;
        }

        if (maxDistanceKm <= 0)
        {
            maxDistanceKm = 25;
        }


        _currentProfile.FullName = fullName;
        _currentProfile.Title = title;
        _currentProfile.Location = location;
        _currentProfile.Email = email;
        _currentProfile.About = about;
        _currentProfile.PhotoPath = _selectedPhotoPath ?? _currentProfile.PhotoPath;
        _currentProfile.Skills = skills;
        _currentProfile.DesiredContractType = desiredContractType;
        _currentProfile.ExperienceLevel = experienceLevel;
        _currentProfile.DesiredSalaryMin = desiredSalaryMin;
        _currentProfile.DesiredSalaryMax = desiredSalaryMax;
        _currentProfile.MaxDistanceKm = maxDistanceKm;
        _currentProfile.Latitude = _selectedLatitude;
        _currentProfile.Longitude = _selectedLongitude;

        await _candidateProfileRepository.UpdateCandidateProfileAsync(_currentProfile);

        await DisplayAlert("Succès", "Profil mis à jour.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
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

    private async void ChangePhoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choisir une photo de profil"
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
            SetProfilePhoto(localPath);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Impossible de sélectionner la photo : {ex.Message}", "OK");
        }
    }

    private async void UseCurrentLocation_Clicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Localisation", "L'autorisation de localisation est nécessaire.", "OK");
                return;
            }

            var request = new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10));

            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location == null)
            {
                await DisplayAlert("Localisation", "Impossible de récupérer ta position.", "OK");
                return;
            }

            _selectedLatitude = location.Latitude;
            _selectedLongitude = location.Longitude;

            UpdateCoordinatesLabel();

            await DisplayAlert("Localisation", "Position enregistrée pour ton profil.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Impossible de récupérer la position : {ex.Message}", "OK");
        }
    }

    private void UpdateCoordinatesLabel()
    {
        if (_selectedLatitude == 0 && _selectedLongitude == 0)
        {
            lblCurrentCoordinates.Text = "Position non définie";
            return;
        }

        lblCurrentCoordinates.Text =
            $"Position définie : {_selectedLatitude:F4}, {_selectedLongitude:F4}";
    }

}
