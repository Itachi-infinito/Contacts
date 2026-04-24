namespace SparkWork2.Views.Shared;

public partial class SuccessPopup : ContentPage
{
    public bool Confirmed { get; private set; }

    public SuccessPopup(string message)
    {
        InitializeComponent();
        lblMessage.Text = message;
    }

    private async void OnOk(object sender, EventArgs e)
    {
        Confirmed = true;
        await Navigation.PopModalAsync();
    }
}