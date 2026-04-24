namespace SparkWork2.Views.Shared;

public partial class DeleteConfirmationPopup : ContentPage
{
    public TaskCompletionSource<bool> CompletionSource { get; } = new();

    public DeleteConfirmationPopup(string message)
    {
        InitializeComponent();
        lblMessage.Text = message;
    }

    private async void OnCancel(object sender, EventArgs e)
    {
        CompletionSource.TrySetResult(false);
        await Navigation.PopModalAsync();
    }

    private async void OnConfirm(object sender, EventArgs e)
    {
        CompletionSource.TrySetResult(true);
        await Navigation.PopModalAsync();
    }
}