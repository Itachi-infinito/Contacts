using Microsoft.Extensions.DependencyInjection;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Public;

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
        lblCurrentRole.Text = _sessionService.CurrentUserRole;
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private void Logout_Clicked(object sender, EventArgs e)
    {
        _sessionService.ClearSession();
        Application.Current.MainPage = new NavigationPage(new WelcomePage());
    }

    private async void DeleteAccount_Clicked(object sender, EventArgs e)
    {
        var popup = new DeleteConfirmationPopup("Delete your account permanently?");
        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;
        if (!confirmed)
            return;

        int userId = _sessionService.CurrentUserId;
        string role = _sessionService.CurrentUserRole;

        await _accountCleanupRepository.DeleteAccountCompletelyAsync(userId, role);

        _sessionService.ClearSession();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Application.Current.MainPage = new NavigationPage(new WelcomePage());
        });
    }
}