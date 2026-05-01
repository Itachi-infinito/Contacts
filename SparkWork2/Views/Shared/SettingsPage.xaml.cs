using Microsoft.Extensions.DependencyInjection;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Candidate;
using SparkWork2.Views.Public;
using SparkWork2.Views.Recruiter;

namespace SparkWork2.Views.Shared;

public partial class SettingsPage : ContentPage
{
    private readonly SessionService _sessionService;
    private readonly AccountCleanupRepository _accountCleanupRepository;

    public SettingsPage(
        SessionService sessionService,
        AccountCleanupRepository accountCleanupRepository)
    {
        InitializeComponent();

        _sessionService = sessionService;
        _accountCleanupRepository = accountCleanupRepository;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        lblCurrentUser.Text = _sessionService.CurrentUserName;
        lblCurrentRole.Text = FormatRole(_sessionService.CurrentUserRole);

        var initials = BuildInitials(_sessionService.CurrentUserName);
        lblHeaderInitials.Text = initials;
        lblAccountInitials.Text = initials;
    }

    private static string FormatRole(string role)
    {
        return role?.ToLowerInvariant() switch
        {
            "candidate" => "Candidat",
            "recruiter" => "Recruteur",
            _ => "Utilisateur"
        };
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

    private async void Back_Clicked(object sender, EventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(CandidateProfilePage)}");
    }

    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(CandidateSwipePage)}");
    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterMatchesPage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(MatchesPage)}");
    }

    private async void Logout_Clicked(object sender, EventArgs e)
    {
        _sessionService.ClearSession();

        var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current.MainPage = appShell;

        await appShell.GoToAsync($"//{nameof(WelcomePage)}");
    }

    private async void DeleteAccount_Clicked(object sender, EventArgs e)
    {
        var popup = new DeleteConfirmationPopup("Supprimer ton compte définitivement ?");
        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;

        if (!confirmed)
            return;

        int userId = _sessionService.CurrentUserId;
        string role = _sessionService.CurrentUserRole;

        await _accountCleanupRepository.DeleteAccountCompletelyAsync(userId, role);

        _sessionService.ClearSession();

        var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current.MainPage = appShell;

        await appShell.GoToAsync($"//{nameof(WelcomePage)}");
    }
}
