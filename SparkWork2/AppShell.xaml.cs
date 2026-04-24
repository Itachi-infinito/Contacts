using SparkWork2.Services;
using SparkWork2.Views.Candidate;
using SparkWork2.Views.Public;
using SparkWork2.Views.Recruiter;
using SparkWork2.Views.Shared;

namespace SparkWork2;

public partial class AppShell : Shell
{
    private readonly SessionService _sessionService;

    public AppShell(SessionService sessionService)
    {
        InitializeComponent();

        _sessionService = sessionService;

        Routing.RegisterRoute(nameof(SplashPage), typeof(SplashPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(RoleSelectionPage), typeof(RoleSelectionPage));
        Routing.RegisterRoute(nameof(RegisterCandidatePage), typeof(RegisterCandidatePage));
        Routing.RegisterRoute(nameof(RegisterRecruiterPage), typeof(RegisterRecruiterPage));

        Routing.RegisterRoute(nameof(CandidateSwipePage), typeof(CandidateSwipePage));
        Routing.RegisterRoute(nameof(JobOfferListPage), typeof(JobOfferListPage));
        Routing.RegisterRoute(nameof(JobOfferDetailPage), typeof(JobOfferDetailPage));
        Routing.RegisterRoute(nameof(MatchesPage), typeof(MatchesPage));
        Routing.RegisterRoute(nameof(CandidateProfilePage), typeof(CandidateProfilePage));
        Routing.RegisterRoute(nameof(EditCandidateProfilePage), typeof(EditCandidateProfilePage));

        Routing.RegisterRoute(nameof(RecruiterSwipePage), typeof(RecruiterSwipePage));
        Routing.RegisterRoute(nameof(AddJobOfferPage), typeof(AddJobOfferPage));
        Routing.RegisterRoute(nameof(EditJobOfferPage), typeof(EditJobOfferPage));
        Routing.RegisterRoute(nameof(RecruiterJobOffersPage), typeof(RecruiterJobOffersPage));
        Routing.RegisterRoute(nameof(RecruiterProfilePage), typeof(RecruiterProfilePage));
        Routing.RegisterRoute(nameof(EditRecruiterProfilePage), typeof(EditRecruiterProfilePage));
        Routing.RegisterRoute(nameof(BrowseCandidatesPage), typeof(BrowseCandidatesPage));
        Routing.RegisterRoute(nameof(RecruiterMatchesPage), typeof(RecruiterMatchesPage));
        Routing.RegisterRoute(nameof(LikesReceivedPage), typeof(LikesReceivedPage));
        Routing.RegisterRoute(nameof(CandidateDetailPage), typeof(CandidateDetailPage));

        Routing.RegisterRoute(nameof(MessagesPage), typeof(MessagesPage));
        Routing.RegisterRoute(nameof(ConversationDetailPage), typeof(ConversationDetailPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

        Routing.RegisterRoute(nameof(MatchPage), typeof(MatchPage));

        UpdateFlyoutByRole();
    }

    public void UpdateFlyoutByRole()
    {
        foreach (var item in Items)
        {
            item.FlyoutItemIsVisible = false;
        }

        if (!_sessionService.IsLoggedIn)
        {
            var welcome = Items.FirstOrDefault(x => x.Route == "welcomeFlyout");
            if (welcome != null)
                welcome.FlyoutItemIsVisible = false;

            FlyoutBehavior = FlyoutBehavior.Disabled;
            return;
        }

        FlyoutBehavior = FlyoutBehavior.Flyout;

        if (_sessionService.CurrentUserRole == "Candidate")
        {
            SetVisible("candidateHome");
            SetVisible("candidateMatches");
            SetVisible("messages");
            SetVisible("candidateProfile");
            SetVisible("settings");
        }
        else if (_sessionService.CurrentUserRole == "Recruiter")
        {
            SetVisible("recruiterHome");
            SetVisible("recruiterJobOffers");
            SetVisible("addJobOffer");
            SetVisible("recruiterMatches");
            SetVisible("likesReceived");
            SetVisible("messages");
            SetVisible("recruiterProfile");
            SetVisible("settings");
        }
    }

    private void SetVisible(string route)
    {
        var item = Items.FirstOrDefault(x => x.Route == route);
        if (item != null)
            item.FlyoutItemIsVisible = true;
    }
}