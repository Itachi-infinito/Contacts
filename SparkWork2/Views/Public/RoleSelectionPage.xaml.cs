namespace SparkWork2.Views.Public;

public partial class RoleSelectionPage : ContentPage
{
    public RoleSelectionPage()
    {
        InitializeComponent();
    }

    private async void Candidate_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterCandidatePage));
    }

    private async void Recruiter_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterRecruiterPage));
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
