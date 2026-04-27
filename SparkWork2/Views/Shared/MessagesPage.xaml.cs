using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;

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
        await LoadMessages();
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
        {
            await OpenConversationAsync(selectedConversation);
        }
    }

    private async void OpenConversation_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is ConversationItem selectedConversation)
        {
            await OpenConversationAsync(selectedConversation);
        }
    }

    private async void DeleteConversation_Clicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton button ||
            button.CommandParameter is not ConversationItem selectedConversation)
            return;

        await ConfirmAndDeleteConversationAsync(selectedConversation);
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
        var popup = new DeleteConfirmationPopup("Remove this conversation?");
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

        if (Math.Abs(bubble.Scale - 1) < 0.01)
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

    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(SparkWork2.Views.Recruiter.RecruiterSwipePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(SparkWork2.Views.Candidate.CandidateSwipePage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(SparkWork2.Views.Recruiter.RecruiterMatchesPage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(SparkWork2.Views.Candidate.MatchesPage)}");
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}
