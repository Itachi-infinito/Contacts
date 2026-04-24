using SparkWork2.Services;
using SparkWork2.Views.Candidate;
using SparkWork2.Views.Recruiter;

namespace SparkWork2.Views.Shared;

[QueryProperty(nameof(ParticipantId), "participantId")]
[QueryProperty(nameof(ParticipantName), "participantName")]
public partial class MatchPage : ContentPage
{
    private readonly SessionService _sessionService;

    private int _participantId;
    private string _participantName = string.Empty;

    public MatchPage(SessionService sessionService)
    {
        InitializeComponent();
        _sessionService = sessionService;
    }

    public string ParticipantId
    {
        set
        {
            if (int.TryParse(value, out int id))
                _participantId = id;
        }
    }

    public string ParticipantName
    {
        set
        {
            _participantName = Uri.UnescapeDataString(value ?? string.Empty);
            lblParticipantName.Text = _participantName;
            lblMatchText.Text = $"You matched with {_participantName}!";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PlayEntranceAnimation();
        await PlayConfettiAnimation();
    }

    private async Task PlayEntranceAnimation()
    {
        lblBrand.Opacity = 0;
        lblCongrats.Opacity = 0;
        lblCongrats.Scale = 0.8;
        lblMatchText.Opacity = 0;
        matchCard.Opacity = 0;
        matchCard.TranslationY = 30;
        btnMessage.Opacity = 0;
        btnKeepSwiping.Opacity = 0;

        await lblBrand.FadeTo(1, 250);
        await lblCongrats.FadeTo(1, 250);
        await lblCongrats.ScaleTo(1, 300, Easing.SpringOut);
        await lblMatchText.FadeTo(1, 250);
        await matchCard.FadeTo(1, 300);
        await matchCard.TranslateTo(0, 0, 300, Easing.CubicOut);
        await btnMessage.FadeTo(1, 250);
        await btnKeepSwiping.FadeTo(1, 250);
    }

    private async Task PlayConfettiAnimation()
    {
        var confettis = new VisualElement[]
        {
            confetti1, confetti2, confetti3, confetti4,
            confetti5, confetti6, confetti7, confetti8,
            confetti9, confetti10, confetti11, confetti12
        };

        foreach (var confetti in confettis)
        {
            confetti.Opacity = 1;
            confetti.TranslationY = -40;
            confetti.Rotation = 0;
        }

        var tasks = new List<Task>();
        var random = new Random();

        foreach (var confetti in confettis)
        {
            uint duration = (uint)random.Next(1400, 2300);
            double targetY = random.Next(450, 750);
            double rotation = random.Next(180, 720);

            tasks.Add(Task.WhenAll(
                confetti.TranslateTo(0, targetY, duration, Easing.CubicIn),
                confetti.RotateTo(rotation, duration, Easing.Linear),
                FadeOutLate(confetti, duration)
            ));
        }

        await Task.WhenAll(tasks);
    }

    private async Task FadeOutLate(VisualElement element, uint totalDuration)
    {
        await Task.Delay((int)(totalDuration * 0.65));
        await element.FadeTo(0, totalDuration / 3);
    }

    private async void SendMessage_Clicked(object sender, EventArgs e)
    {
        if (_participantId == 0 || string.IsNullOrWhiteSpace(_participantName))
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(ConversationDetailPage)}" +
            $"?participantId={_participantId}" +
            $"&participantName={Uri.EscapeDataString(_participantName)}");
    }

    private async void KeepSwiping_Clicked(object sender, EventArgs e)
    {
        if (_sessionService.CurrentUserRole == "Recruiter")
            await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
        else
            await Shell.Current.GoToAsync($"//{nameof(CandidateSwipePage)}");
    }
}