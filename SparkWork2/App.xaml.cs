using SparkWork2.Services;
using SparkWork2.Views.Public;

namespace SparkWork2;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;
    private readonly SplashPage _splashPage;

    public App(DatabaseService databaseService, SplashPage splashPage)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _splashPage = splashPage;

        MainPage = _splashPage;

        _ = InitializeAppAsync();
    }

    private async Task InitializeAppAsync()
    {
        try
        {
            await _databaseService.Init();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database init error: {ex.Message}");
        }
    }
}
