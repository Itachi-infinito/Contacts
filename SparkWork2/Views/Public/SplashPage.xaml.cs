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
        await Task.Delay(900);

        var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
        Application.Current.MainPage = appShell;

        await appShell.GoToAsync($"//{nameof(WelcomePage)}");
    }

    private async Task AnimateLogo()
    {
        LogoMark.Scale = 0.72;
        LogoMark.Rotation = -8;
        LogoMark.Opacity = 0;

        StarIcon.Scale = 0.7;
        StarIcon.Rotation = -25;

        TitleLabel.Opacity = 0;
        TitleLabel.TranslationY = 18;

        SubtitleLabel.Opacity = 0;
        SubtitleLabel.TranslationY = 10;

        await Task.WhenAll(
            LogoMark.FadeTo(1, 360, Easing.CubicOut),
            LogoMark.ScaleTo(1.08, 460, Easing.CubicOut),
            LogoMark.RotateTo(0, 460, Easing.CubicOut),
            StarIcon.RotateTo(0, 520, Easing.CubicOut),
            StarIcon.ScaleTo(1, 460, Easing.CubicOut)
        );

        await LogoMark.ScaleTo(1, 140, Easing.CubicOut);

        await Task.WhenAll(
            TitleLabel.FadeTo(1, 420, Easing.CubicOut),
            TitleLabel.TranslateTo(0, 0, 420, Easing.CubicOut)
        );

        await Task.WhenAll(
            SubtitleLabel.FadeTo(1, 320, Easing.CubicOut),
            SubtitleLabel.TranslateTo(0, 0, 320, Easing.CubicOut)
        );
    }
}
