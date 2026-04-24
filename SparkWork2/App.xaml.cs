using SparkWork2.Services;

namespace SparkWork2;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;
    private readonly AppShell _appShell;

    public App(DatabaseService databaseService, AppShell appShell)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _appShell = appShell;

        MainPage = _appShell;

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