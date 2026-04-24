using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;

namespace SparkWork2.Views.Recruiter;

[QueryProperty(nameof(JobOfferId), "id")]
public partial class EditJobOfferPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;
    private JobOffer jobOffer;

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
            return;

        if (jobOffer.RecruiterUserId != _sessionService.CurrentUserId)
        {
            await DisplayAlert("Error", "You are not allowed to edit this offer.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        entryTitle.Text = jobOffer.Title;
        entryCompany.Text = jobOffer.CompanyName;
        entryLocation.Text = jobOffer.Location;
        entryContractType.Text = jobOffer.ContractType;
        editorDescription.Text = jobOffer.Description;
    }

    private async void Update_Clicked(object sender, EventArgs e)
    {
        if (jobOffer == null)
            return;

        string title = entryTitle.Text?.Trim() ?? "";
        string company = entryCompany.Text?.Trim() ?? "";
        string location = entryLocation.Text?.Trim() ?? "";
        string contractType = entryContractType.Text?.Trim() ?? "";
        string description = editorDescription.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(company) ||
            string.IsNullOrWhiteSpace(location) ||
            string.IsNullOrWhiteSpace(contractType) ||
            string.IsNullOrWhiteSpace(description))
        {
            await DisplayAlert("Error", "Please fill in all fields.", "OK");
            return;
        }

        if (title.Length < 3)
        {
            await DisplayAlert("Error", "Job title must contain at least 3 characters.", "OK");
            return;
        }

        if (description.Length < 10)
        {
            await DisplayAlert("Error", "Description must contain at least 10 characters.", "OK");
            return;
        }

        jobOffer.Title = title;
        jobOffer.CompanyName = company;
        jobOffer.Location = location;
        jobOffer.ContractType = contractType;
        jobOffer.Description = description;

        await _jobOfferRepository.UpdateJobOfferAsync(jobOffer.JobOfferId, jobOffer);

        await DisplayAlert("Success", "Job offer updated successfully.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}