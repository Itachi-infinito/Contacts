using SparkWork2.Models;
using SparkWork2.Repositories;

namespace SparkWork2.Views.Candidate;

public partial class JobOfferListPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;

    public JobOfferListPage(JobOfferRepository jobOfferRepository)
    {
        InitializeComponent();
        _jobOfferRepository = jobOfferRepository;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadJobOffers();
    }

    private async Task LoadJobOffers()
    {
        jobOffersCollection.ItemsSource = null;
        jobOffersCollection.ItemsSource = await _jobOfferRepository.GetJobOffersAsync();
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        jobOffersCollection.ItemsSource =
            await _jobOfferRepository.SearchJobOffersAsync(e.NewTextValue);
    }

    private async void JobOffer_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is JobOffer selectedJobOffer)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(JobOfferDetailPage)}?id={selectedJobOffer.JobOfferId}");
        }
    }
}