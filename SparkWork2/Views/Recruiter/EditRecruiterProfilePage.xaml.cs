using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;

namespace SparkWork2.Views.Recruiter;

public partial class EditRecruiterProfilePage : ContentPage
{
    private readonly RecruiterProfileRepository _recruiterProfileRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;

    private RecruiterProfile? _profile;
    private string _selectedCompanyPhotoPath = string.Empty;
    private int _offersCount;

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

        WireCompletionTracking();
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

        _profile = await _recruiterProfileRepository.GetRecruiterProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        entryCompanyName.Text = string.IsNullOrWhiteSpace(_profile.CompanyName)
            ? _sessionService.CurrentUserName
            : _profile.CompanyName;

        entrySector.Text = _profile.Sector;
        entryWebsite.Text = _profile.Website;
        entryLocation.Text = _profile.Location;
        entryAddress.Text = _profile.Address;
        editorDescription.Text = _profile.Description;
        entryContactEmail.Text = string.IsNullOrWhiteSpace(_profile.ContactEmail)
            ? _sessionService.CurrentUserEmail
            : _profile.ContactEmail;
        entryPhone.Text = _profile.Phone;

        switchProfileVisible.IsToggled = _profile.IsProfileVisible;
        switchShowSector.IsToggled = _profile.ShowSector;
        switchShowLocation.IsToggled = _profile.ShowLocation;

        _selectedCompanyPhotoPath = _profile.CompanyPhotoPath ?? string.Empty;

        var initials = BuildInitials(entryCompanyName.Text);
        lblHeaderInitials.Text = initials;
        lblCompanyInitials.Text = initials;

        SetCompanyPhoto(_selectedCompanyPhotoPath);

        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);
        _offersCount = offers.Count;

        UpdateSectorChip();
        UpdateDescriptionCounter();
        UpdateProfileCompletion();
    }

    private void WireCompletionTracking()
    {
        entryCompanyName.TextChanged += (_, _) =>
        {
            var initials = BuildInitials(entryCompanyName.Text);
            lblHeaderInitials.Text = initials;
            lblCompanyInitials.Text = initials;
            UpdateProfileCompletion();
        };

        entrySector.TextChanged += (_, _) =>
        {
            UpdateSectorChip();
            UpdateProfileCompletion();
        };

        entryLocation.TextChanged += (_, _) => UpdateProfileCompletion();
        editorDescription.TextChanged += (_, _) => UpdateProfileCompletion();
    }

    private async void ChangePhoto_Clicked(object sender, EventArgs e)
    {
        await PickCompanyPhotoAsync();
    }

    private async void ChangePhoto_Tapped(object sender, TappedEventArgs e)
    {
        await PickCompanyPhotoAsync();
    }

    private async Task PickCompanyPhotoAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choisir une photo"
            });

            if (result == null)
                return;

            var extension = Path.GetExtension(result.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var fileName = $"recruiter_{_sessionService.CurrentUserId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var destinationPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            await using var sourceStream = await result.OpenReadAsync();
            await using var destinationStream = File.OpenWrite(destinationPath);
            await sourceStream.CopyToAsync(destinationStream);

            _selectedCompanyPhotoPath = destinationPath;
            SetCompanyPhoto(_selectedCompanyPhotoPath);
            UpdateProfileCompletion();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Photo", $"Impossible de choisir la photo : {ex.Message}", "OK");
        }
    }

    private async void UseCurrentLocation_Clicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

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
                await DisplayAlert("Localisation", "Impossible de récupérer la position.", "OK");
                return;
            }

            var placemarks = await Geocoding.Default.GetPlacemarksAsync(
                location.Latitude,
                location.Longitude);

            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                entryLocation.Text =
                    placemark.Locality ??
                    placemark.SubAdminArea ??
                    placemark.AdminArea ??
                    entryLocation.Text;

                var addressParts = new[]
                {
                    placemark.Thoroughfare,
                    placemark.SubThoroughfare,
                    placemark.PostalCode
                }.Where(x => !string.IsNullOrWhiteSpace(x));

                var address = string.Join(" ", addressParts);

                if (!string.IsNullOrWhiteSpace(address))
                    entryAddress.Text = address;
            }

            await DisplayAlert("Localisation", "Position actuelle utilisée.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Impossible de récupérer la position : {ex.Message}", "OK");
        }
    }

    private void RemoveSector_Tapped(object sender, TappedEventArgs e)
    {
        entrySector.Text = string.Empty;
    }

    private void AddSector_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entrySector.Text))
            entrySector.Text = "Horeca";

        UpdateSectorChip();
    }

    private void Description_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(editorDescription.Text) &&
            editorDescription.Text.Length > 500)
        {
            editorDescription.Text = editorDescription.Text[..500];
            editorDescription.CursorPosition = 500;
        }

        UpdateDescriptionCounter();
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        string companyName = entryCompanyName.Text?.Trim() ?? string.Empty;
        string sector = entrySector.Text?.Trim() ?? string.Empty;
        string website = entryWebsite.Text?.Trim() ?? string.Empty;
        string location = entryLocation.Text?.Trim() ?? string.Empty;
        string address = entryAddress.Text?.Trim() ?? string.Empty;
        string description = editorDescription.Text?.Trim() ?? string.Empty;
        string email = entryContactEmail.Text?.Trim() ?? string.Empty;
        string phone = entryPhone.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(companyName))
        {
            await DisplayAlert("Erreur", "Le nom de l'entreprise est obligatoire.", "OK");
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
        {
            await DisplayAlert("Erreur", "L'adresse email n'est pas valide.", "OK");
            return;
        }

        _profile ??= new RecruiterProfile
        {
            RecruiterId = _sessionService.CurrentUserId
        };

        _profile.CompanyName = companyName;
        _profile.Sector = sector;
        _profile.Website = website;
        _profile.Location = location;
        _profile.Address = address;
        _profile.Description = description;
        _profile.ContactEmail = string.IsNullOrWhiteSpace(email)
            ? _sessionService.CurrentUserEmail
            : email;
        _profile.Phone = phone;
        _profile.CompanyPhotoPath = _selectedCompanyPhotoPath;
        _profile.IsProfileVisible = switchProfileVisible.IsToggled;
        _profile.ShowSector = switchShowSector.IsToggled;
        _profile.ShowLocation = switchShowLocation.IsToggled;

        await _recruiterProfileRepository.UpdateRecruiterProfileAsync(_profile);

        await DisplayAlert("Succès", "Profil mis à jour.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
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

    private void UpdateSectorChip()
    {
        string sector = entrySector.Text?.Trim() ?? string.Empty;

        sectorChipFrame.IsVisible = !string.IsNullOrWhiteSpace(sector);
        lblSectorChip.Text = Capitalize(sector);
    }

    private void UpdateDescriptionCounter()
    {
        int length = editorDescription.Text?.Length ?? 0;
        lblDescriptionCounter.Text = $"{length} / 500 caractères";
    }

    private void UpdateProfileCompletion()
    {
        bool hasPhoto = true;
        bool hasSector = !string.IsNullOrWhiteSpace(entrySector.Text);
        bool hasLocation = !string.IsNullOrWhiteSpace(entryLocation.Text);
        bool hasDescription = !string.IsNullOrWhiteSpace(editorDescription.Text);
        bool hasOffers = _offersCount > 0;

        int completed = 0;
        if (hasPhoto) completed++;
        if (hasSector) completed++;
        if (hasLocation) completed++;
        if (hasDescription) completed++;
        if (hasOffers) completed++;

        int percent = (int)Math.Round(completed * 100.0 / 5);

        lblProfileCompletionPercent.Text = $"{percent}%";
        profileCompletionProgress.Progress = percent / 100.0;

        SetCompletionChip(photoStepChip, lblPhotoStep, "Photo", hasPhoto);
        SetCompletionChip(sectorStepChip, lblSectorStep, "Secteur", hasSector);
        SetCompletionChip(locationStepChip, lblLocationStep, "Localisation", hasLocation);
        SetCompletionChip(descriptionStepChip, lblDescriptionStep, "Description", hasDescription);
        SetCompletionChip(offersStepChip, lblOffersStep, "Offres", hasOffers);
    }

    private static void SetCompletionChip(Frame chip, Label label, string text, bool isDone)
    {
        chip.BackgroundColor = isDone
            ? Color.FromArgb("#DCFCE7")
            : Color.FromArgb("#F0EAFE");

        label.Text = isDone ? $"✓ {text}" : $"+ {text}";
        label.TextColor = isDone
            ? Color.FromArgb("#047857")
            : Color.FromArgb("#7C4DFF");
    }

    private static string BuildInitials(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "M";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();

        if (value.Length == 1)
            return value.ToUpperInvariant();

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
