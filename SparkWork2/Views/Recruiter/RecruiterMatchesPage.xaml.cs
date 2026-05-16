using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterMatchesPage : ContentPage
{
    private readonly MatchRepository _matchRepository;
    private readonly SessionService _sessionService;
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly CompatibilityService _compatibilityService;

    private List<RecruiterMatchDashboardItem> _matches = new();

    public RecruiterMatchesPage(
        MatchRepository matchRepository,
        SessionService sessionService,
        CandidateProfileRepository candidateProfileRepository,
        JobOfferRepository jobOfferRepository,
        CompatibilityService compatibilityService)
    {
        InitializeComponent();

        _matchRepository = matchRepository;
        _sessionService = sessionService;
        _candidateProfileRepository = candidateProfileRepository;
        _jobOfferRepository = jobOfferRepository;
        _compatibilityService = compatibilityService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        lblHeaderInitials.Text = BuildInitials(_sessionService.CurrentUserName);
        await LoadMatches();
    }

    private async Task LoadMatches()
    {
        if (!_sessionService.IsLoggedIn)
        {
            UpdateEmptyState();
            return;
        }

        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);
        var candidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);

        var recruiterMatches = matches
            .Where(match => match.RecruiterUserId == _sessionService.CurrentUserId)
            .ToList();

        _matches = recruiterMatches
            .Select((match, index) =>
            {
                var candidate = candidates.FirstOrDefault(x => x.CandidateId == match.CandidateUserId);
                var offer = offers.FirstOrDefault(x => x.JobOfferId == match.JobOfferId);

                int score = candidate != null && offer != null
                    ? _compatibilityService.CalculateScore(candidate, offer)
                    : GetFallbackScore(index);

                var skills = SplitValues(candidate?.Skills)
                    .Take(2)
                    .ToList();

                if (!skills.Any() && offer != null)
                    skills = SplitValues(offer.RequiredSkills).Take(2).ToList();

                if (!skills.Any())
                    skills.Add("Horeca");

                return new RecruiterMatchDashboardItem
                {
                    MatchId = match.MatchId,
                    CandidateUserId = match.CandidateUserId,
                    CandidateName = string.IsNullOrWhiteSpace(match.CandidateName)
                        ? "Candidat"
                        : ToTitle(match.CandidateName),
                    JobTitle = string.IsNullOrWhiteSpace(candidate?.Title)
                        ? "Profil candidat"
                        : candidate.Title,
                    RoleLine = $"{(string.IsNullOrWhiteSpace(candidate?.Title) ? "Profil candidat" : candidate.Title)} · Horeca",
                    Initials = BuildInitials(match.CandidateName),
                    CompatibilityScore = score,
                    TimeText = index == 0 ? "Aujourd'hui" : index == 1 ? "Hier" : "Lun.",
                    IsNew = index == 0,
                    Skills = skills,
                    OfferTag = $"📄 {(!string.IsNullOrWhiteSpace(offer?.Title) ? offer.Title : match.JobTitle)}",
                    AvatarColor = GetAvatarColor(index)
                };
            })
            .ToList();

        UpdateStats();
        BindMatches();
    }

    private void UpdateStats()
    {
        int total = _matches.Count;
        int newCount = _matches.Count(x => x.IsNew);
        int discussionCount = Math.Max(0, total - newCount);

        lblTabAllCount.Text = total.ToString();
        lblTabNewCount.Text = newCount.ToString();

        lblTotalMatchesValue.Text = total.ToString();
        lblNewMatchesValue.Text = newCount.ToString();
        lblInDiscussionValue.Text = discussionCount.ToString();

        lblTotalMatchesValue.TextColor = total == 0 ? Color.FromArgb("#D4B8F8") : Color.FromArgb("#7C3AED");
        lblNewMatchesValue.TextColor = total == 0 ? Color.FromArgb("#D4B8F8") : Color.FromArgb("#EC4899");
        lblInDiscussionValue.TextColor = total == 0 ? Color.FromArgb("#D4B8F8") : Color.FromArgb("#059669");
    }

    private void BindMatches()
    {
        bool hasMatches = _matches.Any();

        matchesSection.IsVisible = hasMatches;
        emptyStateLayout.IsVisible = !hasMatches;

        BindableLayout.SetItemsSource(matchesCardsLayout, _matches);
    }

    private void UpdateEmptyState()
    {
        _matches = new List<RecruiterMatchDashboardItem>();
        UpdateStats();
        BindMatches();
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterHomePage)}");
    }

    private async void DiscoverCandidates_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));
    }

    private async Task OpenConversationAsync(RecruiterMatchDashboardItem match)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(ConversationDetailPage)}" +
            $"?participantId={match.CandidateUserId}" +
            $"&participantName={Uri.EscapeDataString(match.CandidateName)}" +
            $"&returnRoute={nameof(RecruiterMatchesPage)}");
    }

    private async void Contact_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is RecruiterMatchDashboardItem match)
        {
            await OpenConversationAsync(match);
        }
    }

    private async void Candidate_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.CommandParameter is RecruiterMatchDashboardItem match)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(CandidateDetailPage)}?candidateId={match.CandidateUserId}");
        }
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "M";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return string.Join(" ",
            value.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private static List<string> SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int GetFallbackScore(int index)
    {
        return index switch
        {
            0 => 100,
            1 => 78,
            2 => 65,
            _ => 60
        };
    }

    private static Color GetAvatarColor(int index)
    {
        string[] colors =
        {
            "#8B5CF6",
            "#DB6BA7",
            "#5AAF82",
            "#F0B84A",
            "#7C4DFF"
        };

        return Color.FromArgb(colors[index % colors.Length]);
    }
}

public class RecruiterMatchDashboardItem
{
    public int MatchId { get; set; }
    public int CandidateUserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string RoleLine { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
    public int CompatibilityScore { get; set; }
    public string CompatibilityScoreText => $"{CompatibilityScore}%";
    public double CompatibilityProgressWidth => Math.Max(8, 170 * CompatibilityScore / 100.0);
    public string TimeText { get; set; } = "Aujourd'hui";
    public bool IsNew { get; set; }
    public List<string> Skills { get; set; } = new();
    public string OfferTag { get; set; } = string.Empty;
    public Color AvatarColor { get; set; } = Color.FromArgb("#7C4DFF");
    public Color CardBorderColor => IsNew ? Color.FromArgb("#D4B8F8") : Color.FromArgb("#DED8F5");
}
