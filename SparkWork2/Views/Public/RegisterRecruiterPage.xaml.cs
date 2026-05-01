using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Recruiter;

namespace SparkWork2.Views.Public;

public partial class RegisterRecruiterPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly RecruiterProfileRepository _recruiterProfileRepository;
    private readonly SessionService _sessionService;

    private string? _selectedCompanyPhotoPath;

    public RegisterRecruiterPage(
        AuthService authService,
        RecruiterProfileRepository recruiterProfileRepository,
        SessionService sessionService)
    {
        InitializeComponent();
        _authService = authService;
        _recruiterProfileRepository = recruiterProfileRepository;
        _sessionService = sessionService;
    }

    private async void CreateAccount_Clicked(object sender, EventArgs e)
    {
        string companyName = entryCompanyName.Text?.Trim() ?? string.Empty;
        string email = entryEmail.Text?.Trim() ?? string.Empty;
        string password = entryPassword.Text ?? string.Empty;
        string confirmPassword = entryConfirmPassword.Text ?? string.Empty;

        string sector = entrySector.Text?.Trim() ?? string.Empty;
        string location = entryLocation.Text?.Trim() ?? string.Empty;
        string description = editorDescription.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(companyName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlert("Erreur", "Merci de remplir tous les champs obligatoires.", "OK");
            return;
        }

        if (companyName.Length < 2)
        {
            await DisplayAlert("Erreur", "Le nom de l'entreprise doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (!IsValidEmail(email))
        {
            await DisplayAlert("Erreur", "Merci d'entrer une adresse email valide.", "OK");
            return;
        }

        if (password.Length < 4)
        {
            await DisplayAlert("Erreur", "Le mot de passe doit contenir au moins 4 caractères.", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Erreur", "Les mots de passe ne correspondent pas.", "OK");
            return;
        }

        var registerResult = await _authService.RegisterUserAsync(
            companyName,
            email,
            password,
            "Recruiter");

        if (!registerResult.Success)
        {
            await DisplayAlert("Erreur", registerResult.ErrorMessage, "OK");
            return;
        }

        var registeredUser = await _authService.LoginAsync(email, password);

        if (registeredUser == null)
        {
            await DisplayAlert("Erreur", "Le compte a été créé, mais la connexion automatique a échoué.", "OK");
            return;
        }

        var profile = new RecruiterProfile
        {
            RecruiterId = registeredUser.UserId,
            CompanyName = companyName,
            Sector = sector,
            Location = location,
            ContactEmail = registeredUser.Email,
            Description = description,
            CompanyPhotoPath = _selectedCompanyPhotoPath ?? string.Empty
        };

        await _recruiterProfileRepository.UpdateRecruiterProfileAsync(profile);

        _sessionService.SetSession(registeredUser);

        var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current.MainPage = appShell;
        appShell.UpdateFlyoutByRole();

        await appShell.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void CompanyName_TextChanged(object sender, TextChangedEventArgs e)
    {
        var initials = BuildInitials(e.NewTextValue);
        lblHeaderInitials.Text = initials;
        lblPhotoInitials.Text = initials;
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "HR";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async void ChoosePhoto_Clicked(object sender, EventArgs e)
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
            imgCompanyPhoto.Source = ImageSource.FromFile(localPath);
            photoImageFrame.IsVisible = true;
            photoPlaceholderFrame.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Impossible de sélectionner la photo : {ex.Message}", "OK");
        }
    }
}
