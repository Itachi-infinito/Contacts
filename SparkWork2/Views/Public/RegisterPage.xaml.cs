namespace SparkWork2.Views.Public;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void Next_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RoleSelectionPage));
    }

    private async void Login_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
