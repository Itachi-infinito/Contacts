using Microsoft.Extensions.DependencyInjection;
using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;
using SparkWork2.Views.Recruiter;

namespace SparkWork2.Views.Recruiter;

public partial class AddJobOfferPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;

    public AddJobOfferPage()
    {
        InitializeComponent();

        _jobOfferRepository = MauiProgram.Services.GetRequiredService<JobOfferRepository>();
        _sessionService = MauiProgram.Services.GetRequiredService<SessionService>();
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
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

        var newJobOffer = new JobOffer
        {
            RecruiterUserId = _sessionService.CurrentUserId,
            Title = title,
            CompanyName = company,
            Location = location,
            ContractType = contractType,
            Description = description
        };

        await _jobOfferRepository.AddJobOfferAsync(newJobOffer);

        var popup = new SuccessPopup("Your job offer has been published successfully.");
        await Navigation.PushModalAsync(popup);

        while (Navigation.ModalStack.Contains(popup))
        {
            await Task.Delay(100);
        }

        if (popup.Confirmed)
        {
            await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
        }
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }
}