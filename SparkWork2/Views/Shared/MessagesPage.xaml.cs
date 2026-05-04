using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Candidate;
using SparkWork2.Views.Recruiter;

namespace SparkWork2.Views.Shared;

public partial class MessagesPage : ContentPage
{
    private readonly MessageRepository _messageRepository;
    private readonly SessionService _sessionService;

    public MessagesPage(MessageRepository messageRepository, SessionService sessionService)
    {
        InitializeComponent();
        _messageRepository = messageRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        UpdateHeader();
        UpdateBottomNavigation();

        await LoadMessages();
    }

    private void UpdateHeader()
    {
        lblHeaderInitials.Text = BuildInitials(_sessionService.CurrentUserName);
    }

    private void UpdateBottomNavigation()
    {
        bool isRecruiter = _sessionService.CurrentUserRole == "Recruiter";

        recruiterBottomNav.IsVisible = isRecruiter;
        candidateBottomNav.IsVisible = !isRecruiter;
    }

    private async Task LoadMessages()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        messagesCollection.ItemsSource = null;
        messagesCollection.ItemsSource =
            await _messageRepository.GetConversationsAsync(_sessionService.CurrentUserId);
    }

    private async Task OpenConversationAsync(ConversationItem selectedConversation)
    {
        if (selectedConversation == null)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(ConversationDetailPage)}" +
            $"?participantId={selectedConversation.ParticipantUserId}" +
            $"&participantName={Uri.EscapeDataString(selectedConversation.ParticipantName)}");
    }

    private async void Message_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ConversationItem selectedConversation)
            await OpenConversationAsync(selectedConversation);
    }

    private async void ConversationBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame ||
            frame.BindingContext is not ConversationItem selectedConversation)
            return;

        await BubbleTapAnimation(frame);
        await OpenConversationAsync(selectedConversation);
    }

    private async void DeleteBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame ||
            frame.BindingContext is not ConversationItem selectedConversation)
            return;

        await BubbleTapAnimation(frame);
        await ConfirmAndDeleteConversationAsync(selectedConversation);
    }

    private async Task ConfirmAndDeleteConversationAsync(ConversationItem selectedConversation)
    {
        var popup = new DeleteConfirmationPopup("Supprimer cette conversation ?");
        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;
        if (!confirmed)
            return;

        await _messageRepository.DeleteConversationAsync(
            _sessionService.CurrentUserId,
            selectedConversation.ParticipantUserId);

        await LoadMessages();
    }

    private async Task BubbleTapAnimation(Frame bubble)
    {
        await bubble.ScaleTo(0.95, 70, Easing.CubicIn);
        await bubble.ScaleTo(1.0, 100, Easing.SpringOut);
    }

    private async void SwipeBubble_Loaded(object sender, EventArgs e)
    {
        if (sender is not VisualElement bubble)
            return;

        await bubble.ScaleTo(1, 220, Easing.SpringOut);
    }

    private async void MessageCard_Loaded(object sender, EventArgs e)
    {
        if (sender is not VisualElement item)
            return;

        if (item.Opacity >= 0.99 &&
            Math.Abs(item.TranslationY) < 0.1 &&
            Math.Abs(item.Scale - 1) < 0.01)
            return;

        await Task.WhenAll(
            item.FadeTo(1, 180, Easing.CubicOut),
            item.TranslateTo(0, 0, 240, Easing.CubicOut),
            item.ScaleTo(1, 260, Easing.SpringOut)
        );
    }

    private async void Home_Clicked(object sender, EventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterHomePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(CandidateHomePage)}");
    }

    private async void Discover_Clicked(object sender, EventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(CandidateSwipePage)}");
    }

    private async void AddOffer_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));
    }

    private async void Profile_Clicked(object sender, EventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(CandidateProfilePage)}");
    }

    private async void Profile_Tapped(object sender, TappedEventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(CandidateProfilePage)}");
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
}
