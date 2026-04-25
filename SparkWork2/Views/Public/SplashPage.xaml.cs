using Microsoft.Extensions.DependencyInjection;
using SparkWork2.Services;

namespace SparkWork2.Views.Public;

public partial class SplashPage : ContentPage
{
    private readonly SessionService _sessionService;
    private bool _animated = false;

    public SplashPage(SessionService sessionService)
    {
        InitializeComponent();
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_animated)
            return;

        _animated = true;

        await AnimateLogo();
        await Task.Delay(1200);

        var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current.MainPage = appShell;

        await appShell.GoToAsync($"//{nameof(WelcomePage)}");
    }

    private async Task AnimateLogo()
    {
        StarIcon.Scale = 0.5;
        StarIcon.Rotation = -30;
        StarIcon.Opacity = 0;

        TitleLabel.Opacity = 0;
        TitleLabel.TranslationY = 20;

        await Task.WhenAll(
            StarIcon.FadeTo(1, 400),
            StarIcon.RotateTo(0, 500, Easing.CubicOut),
            StarIcon.ScaleTo(1.1, 500, Easing.CubicOut)
        );

        await StarIcon.ScaleTo(1.0, 150, Easing.CubicOut);

        await Task.WhenAll(
            TitleLabel.FadeTo(1, 600),
            TitleLabel.TranslateTo(0, 0, 600, Easing.CubicOut)
        );

        await StarIcon.FadeTo(0.7, 150);
        await StarIcon.FadeTo(1, 150);
    }
}