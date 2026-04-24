using SparkWork2.Repositories;
using SparkWork2.Services;
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
                LoadConversation();
            }
        }
    }

    public string ParticipantName
    {
        set
        {
            participantName = Uri.UnescapeDataString(value);
            lblConversationTitle.Text = participantName;
        }
    }

    private async void LoadConversation()
    {
        if (participantUserId <= 0 || !_sessionService.IsLoggedIn)
            return;

        var messages = await _messageRepository.GetConversationAsync(
            _sessionService.CurrentUserId,
            participantUserId);

        conversationCollection.ItemsSource = null;
        conversationCollection.ItemsSource = messages;

        if (messages.Any())
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                conversationCollection.ScrollTo(messages.Last(), position: ScrollToPosition.End, animate: true);
            });
        }
    }

    private async void Send_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(editorNewMessage.Text))
            return;

        if (!_sessionService.IsLoggedIn)
            return;

        await _messageRepository.AddMessageAsync(
            _sessionService.CurrentUserId,
            participantUserId,
            _sessionService.CurrentUserName,
            participantName,
            editorNewMessage.Text);

        editorNewMessage.Text = string.Empty;
        LoadConversation();
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        var route = string.IsNullOrWhiteSpace(ReturnRoute)
            ? nameof(MessagesPage)
            : ReturnRoute;

        await Shell.Current.GoToAsync($"//{route}");
    }
}