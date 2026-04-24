using System.Text.RegularExpressions;
using SparkWork2.Services;
using SparkWork2.Views.Candidate;
using SparkWork2.Views.Recruiter;

namespace SparkWork2.Views.Public;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly SessionService _sessionService;

    public LoginPage(AuthService authService, SessionService sessionService)
    {
        InitializeComponent();

        _authService = authService;
        _sessionService = sessionService;
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        string email = entryEmail.Text?.Trim() ?? string.Empty;
        string password = entryPassword.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please enter your email and password.", "OK");
            return;
        }

        if (!IsValidEmail(email))
        {
            await DisplayAlert("Error", "Please enter a valid email address.", "OK");
            return;
        }

        var user = await _authService.LoginAsync(email, password);

        if (user == null)
        {
            await DisplayAlert("Login failed", "Invalid email or password.", "OK");
            return;
        }

        _sessionService.SetSession(user);

        var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current.MainPage = appShell;
        appShell.UpdateFlyoutByRole();

        if (user.Role == "Candidate")
        {
            await appShell.GoToAsync($"//{nameof(CandidateSwipePage)}");
        }
        else
        {
            await appShell.GoToAsync($"//{nameof(RecruiterSwipePage)}");
        }
    }

    private async void SignUp_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}