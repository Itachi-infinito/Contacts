using Microsoft.Extensions.DependencyInjection;

namespace SparkWork2.Views.Public;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        var loginPage = MauiProgram.Services.GetRequiredService<LoginPage>();
        await Navigation.PushAsync(loginPage);
    }

    private async void Register_Clicked(object sender, EventArgs e)
    {
        var registerPage = MauiProgram.Services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}