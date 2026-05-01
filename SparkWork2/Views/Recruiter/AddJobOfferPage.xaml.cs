using Microsoft.Extensions.DependencyInjection;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class AddJobOfferPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;
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
        entryRequiredSkills.Text = string.Empty;
        entryNiceToHaveSkills.Text = string.Empty;

    }

    public AddJobOfferPage()
    {
        InitializeComponent();

        _jobOfferRepository = MauiProgram.Services.GetRequiredService<JobOfferRepository>();
        _sessionService = MauiProgram.Services.GetRequiredService<SessionService>();
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
            NiceToHaveSkills = niceToHaveSkills
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

}
