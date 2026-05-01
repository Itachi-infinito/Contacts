using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Candidate;
using SparkWork2.Views.Recruiter;

namespace SparkWork2.Views.Shared;

[QueryProperty(nameof(ParticipantId), "participantId")]
[QueryProperty(nameof(ParticipantName), "participantName")]
[QueryProperty(nameof(ReturnRoute), "returnRoute")]
public partial class ConversationDetailPage : ContentPage
{
    private readonly MessageRepository _messageRepository;
    private readonly SessionService _sessionService;

    public string ReturnRoute { get; set; } = nameof(MessagesPage);

    private int participantUserId;
    private string participantName = string.Empty;
    private bool _isSending;

    public ConversationDetailPage(MessageRepository messageRepository, SessionService sessionService)
    {
        InitializeComponent();
        _messageRepository = messageRepository;
        _sessionService = sessionService;
    }

    public string ParticipantId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                participantUserId = id;
                _ = LoadConversation();
            }
        }
    }

    public string ParticipantName
    {
        set
        {
            participantName = Uri.UnescapeDataString(value ?? string.Empty);
            lblConversationTitle.Text = participantName;
            lblParticipantInitials.Text = BuildInitials(participantName);
        }
    }

    private async Task LoadConversation()
    {
        if (participantUserId <= 0 || !_sessionService.IsLoggedIn)
            return;

        var messages = await _messageRepository.GetConversationAsync(
            _sessionService.CurrentUserId,
            participantUserId);

        foreach (var message in messages)
        {
            message.IsMine = message.SenderUserId == _sessionService.CurrentUserId;
        }

        conversationCollection.ItemsSource = null;
        conversationCollection.ItemsSource = messages;

        if (messages.Any())
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(120);
                conversationCollection.ScrollTo(messages.Last(), position: ScrollToPosition.End, animate: true);
            });
        }
    }

    private async void Send_Clicked(object sender, EventArgs e)
    {
        await SendMessageAsync();
    }

    private async void Message_Completed(object sender, EventArgs e)
    {
        await SendMessageAsync();
    }

    private async Task SendMessageAsync()
    {
        if (_isSending)
            return;

        var content = editorNewMessage.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            return;

        if (!_sessionService.IsLoggedIn || participantUserId <= 0)
            return;

        _isSending = true;
        btnSend.IsEnabled = false;

        await _messageRepository.AddMessageAsync(
            _sessionService.CurrentUserId,
            participantUserId,
            _sessionService.CurrentUserName,
            participantName,
            content);

        editorNewMessage.Text = string.Empty;
        await LoadConversation();

        btnSend.IsEnabled = true;
        _isSending = false;
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        var route = string.IsNullOrWhiteSpace(ReturnRoute)
            ? nameof(MessagesPage)
            : ReturnRoute;

        await Shell.Current.GoToAsync($"//{route}");
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
