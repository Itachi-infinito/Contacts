using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;


namespace SparkWork2.Views.Recruiter;

public partial class AddJobOfferPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;
    private double _selectedLatitude;
    private double _selectedLongitude;
    private readonly SkillCatalogService _skillCatalogService;

    private readonly List<string> _selectedRequiredSkills = new();
    private readonly List<string> _selectedNiceSkills = new();
    private readonly GeocodingService _geocodingService;



    private void ClearForm()
    {
        entryTitle.Text = string.Empty;
        entryCompany.Text = string.Empty;
        entryLocation.Text = string.Empty;
        entryContractType.Text = string.Empty;
        editorDescription.Text = string.Empty;
        entryAddress.Text = string.Empty;
        entrySalaryMin.Text = string.Empty;
        entrySalaryMax.Text = string.Empty;
        entryLevel.Text = string.Empty;
        entryRemoteMode.Text = string.Empty;
        _selectedRequiredSkills.Clear();
        _selectedNiceSkills.Clear();

        requiredSkillPicker.SelectedItem = null;
        niceSkillPicker.SelectedItem = null;

        RefreshRequiredSkillsLayout();
        RefreshNiceSkillsLayout();


        _selectedLatitude = 0;
        _selectedLongitude = 0;
        UpdateOfferCoordinatesLabel();
        UpdateOfferCompletion();

    }

    public AddJobOfferPage(
    JobOfferRepository jobOfferRepository,
    SessionService sessionService,
    SkillCatalogService skillCatalogService,
    GeocodingService geocodingService)
    {
        InitializeComponent();

        _jobOfferRepository = jobOfferRepository;
        _sessionService = sessionService;
        _skillCatalogService = skillCatalogService;
        _geocodingService = geocodingService;



        requiredSkillPicker.ItemsSource = _skillCatalogService.HorecaSkills.ToList();
        niceSkillPicker.ItemsSource = _skillCatalogService.HorecaSkills.ToList();

        RefreshRequiredSkillsLayout();
        RefreshNiceSkillsLayout();
        lblHeaderInitials.Text = BuildInitials(_sessionService.CurrentUserName);

        WireCompletionTracking();
        UpdateOfferCompletion();

    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        string title = entryTitle.Text?.Trim() ?? string.Empty;
        string company = entryCompany.Text?.Trim() ?? string.Empty;
        string location = entryLocation.Text?.Trim() ?? string.Empty;
        string contractType = entryContractType.Text?.Trim() ?? string.Empty;
        string description = editorDescription.Text?.Trim() ?? string.Empty;
        string address = entryAddress.Text?.Trim() ?? string.Empty;
        string level = entryLevel.Text?.Trim() ?? string.Empty;
        string remoteMode = entryRemoteMode.Text?.Trim() ?? string.Empty;
        string requiredSkills = _skillCatalogService.FormatSkills(_selectedRequiredSkills);
        string niceToHaveSkills = _skillCatalogService.FormatSkills(_selectedNiceSkills);


        int.TryParse(entrySalaryMin.Text?.Trim(), out int salaryMin);
        int.TryParse(entrySalaryMax.Text?.Trim(), out int salaryMax);


        if (string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(company) ||
            string.IsNullOrWhiteSpace(location) ||
            string.IsNullOrWhiteSpace(contractType) ||
            string.IsNullOrWhiteSpace(description))
        {
            await DisplayAlert("Erreur", "Merci de remplir tous les champs.", "OK");
            return;
        }

        if (title.Length < 3)
        {
            await DisplayAlert("Erreur", "Le titre du poste doit contenir au moins 3 caractères.", "OK");
            return;
        }

        if (company.Length < 2)
        {
            await DisplayAlert("Erreur", "Le nom de l'entreprise doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (location.Length < 2)
        {
            await DisplayAlert("Erreur", "La localisation doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (contractType.Length < 2)
        {
            await DisplayAlert("Erreur", "Le type de contrat doit contenir au moins 2 caractères.", "OK");
            return;
        }

        if (description.Length < 10)
        {
            await DisplayAlert("Erreur", "La description doit contenir au moins 10 caractères.", "OK");
            return;
        }
        if (salaryMin < 0 || salaryMax < 0)
        {
            await DisplayAlert("Erreur", "Le salaire ne peut pas être négatif.", "OK");
            return;
        }

        if (salaryMin > 0 && salaryMax > 0 && salaryMin > salaryMax)
        {
            await DisplayAlert("Erreur", "Le salaire minimum ne peut pas être supérieur au salaire maximum.", "OK");
            return;
        }

        if (_selectedLatitude == 0 && _selectedLongitude == 0)
        {
            string geocodingQuery = !string.IsNullOrWhiteSpace(address)
                ? $"{address}, {location}"
                : location;

            var coordinates = await _geocodingService.GeocodeAsync(geocodingQuery);

            if (coordinates != null)
            {
                _selectedLatitude = coordinates.Value.Latitude;
                _selectedLongitude = coordinates.Value.Longitude;
                UpdateOfferCoordinatesLabel();
            }
        }

        var newJobOffer = new JobOffer
        {
            RecruiterUserId = _sessionService.CurrentUserId,
            Title = title,
            CompanyName = company,
            Location = location,
            ContractType = contractType,
            Description = description,
            Address = address,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Level = level,
            RemoteMode = remoteMode,
            RequiredSkills = requiredSkills,
            NiceToHaveSkills = niceToHaveSkills,
            Latitude = _selectedLatitude,
            Longitude = _selectedLongitude
            
        };


        await _jobOfferRepository.AddJobOfferAsync(newJobOffer);

        var popup = new SuccessPopup("Offre publiée avec succès.");
        await Navigation.PushModalAsync(popup);

        while (Navigation.ModalStack.Contains(popup))
        {
            await Task.Delay(100);
        }

        ClearForm();

        await Shell.Current.GoToAsync("..");

    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    private async void UseOfferLocation_Clicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Localisation", "L'autorisation de localisation est nécessaire.", "OK");
                return;
            }

            var request = new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10));

            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location == null)
            {
                await DisplayAlert("Localisation", "Impossible de récupérer la position.", "OK");
                return;
            }

            _selectedLatitude = location.Latitude;
            _selectedLongitude = location.Longitude;

            UpdateOfferCoordinatesLabel();

            await DisplayAlert("Localisation", "Position enregistrée pour cette offre.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Impossible de récupérer la position : {ex.Message}", "OK");
        }
    }

    private void UpdateOfferCoordinatesLabel()
    {
        if (_selectedLatitude == 0 && _selectedLongitude == 0)
        {
            lblOfferCoordinates.Text = "Position de l'offre non définie";
            return;
        }

        lblOfferCoordinates.Text =
            $"Position définie : {_selectedLatitude:F4}, {_selectedLongitude:F4}";
    }
    private void AddRequiredSkill_Clicked(object sender, EventArgs e)
    {
        if (requiredSkillPicker.SelectedItem is not string selectedSkill)
            return;

        if (_selectedRequiredSkills.Any(skill =>
            string.Equals(skill, selectedSkill, StringComparison.OrdinalIgnoreCase)))
            return;

        _selectedRequiredSkills.Add(selectedSkill);
        requiredSkillPicker.SelectedItem = null;

        RefreshRequiredSkillsLayout();
        UpdateOfferCompletion();

    }

    private void AddNiceSkill_Clicked(object sender, EventArgs e)
    {
        if (niceSkillPicker.SelectedItem is not string selectedSkill)
            return;

        if (_selectedNiceSkills.Any(skill =>
            string.Equals(skill, selectedSkill, StringComparison.OrdinalIgnoreCase)))
            return;

        _selectedNiceSkills.Add(selectedSkill);
        niceSkillPicker.SelectedItem = null;

        RefreshNiceSkillsLayout();
        UpdateOfferCompletion();

    }

    private void RefreshRequiredSkillsLayout()
    {
        selectedRequiredSkillsLayout.Children.Clear();
        lblRequiredSkillsEmpty.IsVisible = !_selectedRequiredSkills.Any();

        foreach (var skill in _selectedRequiredSkills)
        {
            selectedRequiredSkillsLayout.Children.Add(CreateSkillBadge(skill, RemoveRequiredSkill));
        }
    }

    private void RefreshNiceSkillsLayout()
    {
        selectedNiceSkillsLayout.Children.Clear();
        lblNiceSkillsEmpty.IsVisible = !_selectedNiceSkills.Any();

        foreach (var skill in _selectedNiceSkills)
        {
            selectedNiceSkillsLayout.Children.Add(CreateSkillBadge(skill, RemoveNiceSkill));
        }
    }

    private Frame CreateSkillBadge(string skill, Action<string?> removeAction)
    {
        return new Frame
        {
            BackgroundColor = Color.FromArgb("#F0EAFE"),
            CornerRadius = 14,
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 0, 8, 8),
            HasShadow = false,
            Content = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
            {
                new Label
                {
                    Text = skill,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#7C4DFF"),
                    VerticalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = "×",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#7C4DFF"),
                    VerticalTextAlignment = TextAlignment.Center,
                    BindingContext = skill,
                    GestureRecognizers =
                    {
                        new TapGestureRecognizer
                        {
                            Command = new Command<string>(removeAction),
                            CommandParameter = skill
                        }
                    }
                }
            }
            }
        };
    }

    private void RemoveRequiredSkill(string? skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
            return;

        _selectedRequiredSkills.RemoveAll(x =>
            string.Equals(x, skill, StringComparison.OrdinalIgnoreCase));

        RefreshRequiredSkillsLayout();
        UpdateOfferCompletion();

    }

    private void RemoveNiceSkill(string? skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
            return;

        _selectedNiceSkills.RemoveAll(x =>
            string.Equals(x, skill, StringComparison.OrdinalIgnoreCase));

        RefreshNiceSkillsLayout();
        UpdateOfferCompletion();

    }

    private void WireCompletionTracking()
    {
        entryTitle.TextChanged += (_, _) => UpdateOfferCompletion();
        entryCompany.TextChanged += (_, _) => UpdateOfferCompletion();
        entryLocation.TextChanged += (_, _) => UpdateOfferCompletion();
        entryContractType.TextChanged += (_, _) => UpdateOfferCompletion();
        entryAddress.TextChanged += (_, _) => UpdateOfferCompletion();
        entrySalaryMin.TextChanged += (_, _) => UpdateOfferCompletion();
        entrySalaryMax.TextChanged += (_, _) => UpdateOfferCompletion();
        entryLevel.TextChanged += (_, _) => UpdateOfferCompletion();
        entryRemoteMode.TextChanged += (_, _) => UpdateOfferCompletion();
        editorDescription.TextChanged += (_, _) => UpdateOfferCompletion();
    }

    private void UpdateOfferCompletion()
    {
        bool hasInfo =
            !string.IsNullOrWhiteSpace(entryTitle.Text) &&
            !string.IsNullOrWhiteSpace(entryCompany.Text) &&
            !string.IsNullOrWhiteSpace(entryLocation.Text) &&
            !string.IsNullOrWhiteSpace(entryContractType.Text);

        bool hasSalary =
            !string.IsNullOrWhiteSpace(entrySalaryMin.Text) ||
            !string.IsNullOrWhiteSpace(entrySalaryMax.Text) ||
            !string.IsNullOrWhiteSpace(entryLevel.Text) ||
            !string.IsNullOrWhiteSpace(entryRemoteMode.Text);

        bool hasSkills = _selectedRequiredSkills.Any() || _selectedNiceSkills.Any();

        bool hasDescription =
            !string.IsNullOrWhiteSpace(editorDescription.Text) &&
            editorDescription.Text.Trim().Length >= 10;

        int completed = 0;
        if (hasInfo) completed++;
        if (hasSalary) completed++;
        if (hasSkills) completed++;
        if (hasDescription) completed++;

        int percent = (int)Math.Round(completed * 100.0 / 4);

        lblOfferCompletionPercent.Text = $"{percent}%";
        offerCompletionProgress.Progress = percent / 100.0;

        SetStepState(infoStepBar, lblInfoStep, hasInfo);
        SetStepState(salaryStepBar, lblSalaryStep, hasSalary);
        SetStepState(skillsStepBar, lblSkillsStep, hasSkills);
        SetStepState(descriptionStepBar, lblDescriptionStep, hasDescription);
    }

    private void SetStepState(BoxView bar, Label label, bool isDone)
    {
        bar.Color = isDone
            ? Color.FromArgb("#7C4DFF")
            : Color.FromArgb("#DDD7F2");

        label.TextColor = isDone
            ? Color.FromArgb("#7C4DFF")
            : Color.FromArgb("#B0ABCE");
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "AL";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

}
