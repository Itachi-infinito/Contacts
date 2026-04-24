using System.Text.RegularExpressions;
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
            await DisplayAlert("Error", "Please fill in all required fields.", "OK");
            return;
        }

        if (companyName.Length < 2)
        {
            await DisplayAlert("Error", "Company name must contain at least 2 characters.", "OK");
            return;
        }

        if (!IsValidEmail(email))
        {
            await DisplayAlert("Error", "Please enter a valid email address.", "OK");
            return;
        }

        if (password.Length < 4)
        {
            await DisplayAlert("Error", "Password must contain at least 4 characters.", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Error", "Passwords do not match.", "OK");
            return;
        }

        var registerResult = await _authService.RegisterUserAsync(
            companyName,
            email,
            password,
            "Recruiter");

        if (!registerResult.Success)
        {
            await DisplayAlert("Error", registerResult.ErrorMessage, "OK");
            return;
        }

        var registeredUser = await _authService.LoginAsync(email, password);

        if (registeredUser == null)
        {
            await DisplayAlert("Error", "Registration succeeded, but automatic login failed.", "OK");
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

        if (Application.Current?.MainPage is AppShell shell)
        {
            shell.UpdateFlyoutByRole();
        }

        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void ChoosePhoto_Clicked(object sender, EventArgs e)
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