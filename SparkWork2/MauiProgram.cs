using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Candidate;
using SparkWork2.Views.Public;
using SparkWork2.Views.Recruiter;
using SparkWork2.Views.Shared;

namespace SparkWork2;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; }

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddTransient<AuthService>();
        builder.Services.AddTransient<AccountCleanupRepository>();
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<BrowseCandidatesPage>();

        builder.Services.AddTransient<CandidateProfileRepository>();
        builder.Services.AddTransient<CandidateSwipePage>();
        builder.Services.AddTransient<CandidateJobLikeRepository>();
        builder.Services.AddTransient<CandidateDetailPage>();

        builder.Services.AddSingleton<DatabaseService>();

        builder.Services.AddTransient<EditRecruiterProfilePage>();

        builder.Services.AddTransient<JobOfferRepository>();
        builder.Services.AddTransient<JobOfferDetailPage>();
        builder.Services.AddTransient<JobOfferListPage>();

        builder.Services.AddTransient<LikesReceivedPage>();
        builder.Services.AddTransient<LoginPage>();

        builder.Services.AddTransient<MatchRepository>();
        builder.Services.AddTransient<MessageRepository>();
        builder.Services.AddTransient<MatchesPage>();

        builder.Services.AddSingleton<PasswordService>();

        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<RegisterCandidatePage>();
        builder.Services.AddTransient<RegisterRecruiterPage>();
        builder.Services.AddTransient<RecruiterProfileRepository>();
        builder.Services.AddTransient<RecruiterProfilePage>();
        builder.Services.AddTransient<RecruiterCandidateLikeRepository>();
        builder.Services.AddTransient<RecruiterSwipePage>();
        builder.Services.AddTransient<RecruiterJobOffersPage>();
        builder.Services.AddTransient<RecruiterMatchesPage>();

        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddTransient<SplashPage>();

        builder.Services.AddTransient<UserRepository>();

        builder.Services.AddTransient<WelcomePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}