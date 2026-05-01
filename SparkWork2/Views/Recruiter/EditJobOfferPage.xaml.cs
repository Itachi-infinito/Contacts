using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;


namespace SparkWork2.Views.Recruiter;

[QueryProperty(nameof(JobOfferId), "id")]
public partial class EditJobOfferPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;
    private JobOffer? jobOffer;
    private double _selectedLatitude;
    private double _selectedLongitude;


    public EditJobOfferPage(JobOfferRepository jobOfferRepository, SessionService sessionService)
    {
        InitializeComponent();
        _jobOfferRepository = jobOfferRepository;
        _sessionService = sessionService;
    }

    public string JobOfferId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                LoadJobOffer(id);
            }
        }
    }

    private async void LoadJobOffer(int id)
    {
        jobOffer = await _jobOfferRepository.GetJobOfferByIdAsync(id);

        if (jobOffer == null)
        {
            await DisplayAlert("Erreur", "Offre introuvable.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (jobOffer.RecruiterUserId != _sessionService.CurrentUserId)
        {
            await DisplayAlert("Erreur", "Tu n'es pas autorisé à modifier cette offre.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        entryTitle.Text = jobOffer.Title;
        entryCompany.Text = jobOffer.CompanyName;
        entryLocation.Text = jobOffer.Location;
        entryContractType.Text = jobOffer.ContractType;
        editorDescription.Text = jobOffer.Description;
        entryAddress.Text = jobOffer.Address;
        entrySalaryMin.Text = jobOffer.SalaryMin > 0 ? jobOffer.SalaryMin.ToString() : string.Empty;
        entrySalaryMax.Text = jobOffer.SalaryMax > 0 ? jobOffer.SalaryMax.ToString() : string.Empty;
        entryLevel.Text = jobOffer.Level;
        entryRemoteMode.Text = jobOffer.RemoteMode;
        entryRequiredSkills.Text = jobOffer.RequiredSkills;
        entryNiceToHaveSkills.Text = jobOffer.NiceToHaveSkills;
        _selectedLatitude = jobOffer.Latitude;
        _selectedLongitude = jobOffer.Longitude;
        UpdateOfferCoordinatesLabel();


    }

    private async void Update_Clicked(object sender, EventArgs e)
    {
        if (jobOffer == null)
            return;

        string title = entryTitle.Text?.Trim() ?? string.Empty;
        string company = entryCompany.Text?.Trim() ?? string.Empty;
        string location = entryLocation.Text?.Trim() ?? string.Empty;
        string contractType = entryContractType.Text?.Trim() ?? string.Empty;
        string description = editorDescription.Text?.Trim() ?? string.Empty;
        string address = entryAddress.Text?.Trim() ?? string.Empty;
        string level = entryLevel.Text?.Trim() ?? string.Empty;
        string remoteMode = entryRemoteMode.Text?.Trim() ?? string.Empty;
        string requiredSkills = entryRequiredSkills.Text?.Trim() ?? string.Empty;
        string niceToHaveSkills = entryNiceToHaveSkills.Text?.Trim() ?? string.Empty;

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


        jobOffer.Title = title;
        jobOffer.CompanyName = company;
        jobOffer.Location = location;
        jobOffer.ContractType = contractType;
        jobOffer.Description = description;
        jobOffer.Address = address;
        jobOffer.SalaryMin = salaryMin;
        jobOffer.SalaryMax = salaryMax;
        jobOffer.Level = level;
        jobOffer.RemoteMode = remoteMode;
        jobOffer.RequiredSkills = requiredSkills;
        jobOffer.NiceToHaveSkills = niceToHaveSkills;
        jobOffer.Latitude = _selectedLatitude;
        jobOffer.Longitude = _selectedLongitude;


        await _jobOfferRepository.UpdateJobOfferAsync(jobOffer.JobOfferId, jobOffer);

        await DisplayAlert("Succès", "Offre mise à jour.", "OK");
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
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

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

}
