using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterJobOffersPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;

    public RecruiterJobOffersPage(
        JobOfferRepository jobOfferRepository,
        SessionService sessionService)
    {
        InitializeComponent();
        _jobOfferRepository = jobOfferRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadJobOffers();
    }

    private async Task LoadJobOffers()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        jobOffersCollection.ItemsSource = null;
        jobOffersCollection.ItemsSource =
            await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);
    }

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is JobOffer selectedJobOffer)
        {
            var popup = new DeleteConfirmationPopup(
    $"Do you really want to delete '{selectedJobOffer.Title}'?");

            await Navigation.PushModalAsync(popup);

            bool confirmed = await popup.CompletionSource.Task;

            if (confirmed)
            {
                await _jobOfferRepository.DeleteJobOfferAsync(selectedJobOffer.JobOfferId);
                await LoadJobOffers();
            }
        }
    }

    private async void Edit_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is JobOffer selectedJobOffer)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(EditJobOfferPage)}?id={selectedJobOffer.JobOfferId}");
        }
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void EditBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not JobOffer selectedJobOffer)
            return;

        await BubbleTapAnimation(frame);

        await Shell.Current.GoToAsync(
            $"{nameof(EditJobOfferPage)}?id={selectedJobOffer.JobOfferId}");
    }

    private async void DeleteBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not JobOffer selectedJobOffer)
            return;

        await BubbleTapAnimation(frame);

        var popup = new DeleteConfirmationPopup("Remove this match?");
        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;
        if (!confirmed)
            return;

        await _jobOfferRepository.DeleteJobOfferAsync(selectedJobOffer.JobOfferId);
        await LoadJobOffers();
    }

    private async void JobOfferCard_Loaded(object sender, EventArgs e)
    {
        if (sender is not Frame card)
            return;

        if (card.Opacity >= 0.99 && Math.Abs(card.TranslationY) < 0.1 && Math.Abs(card.Scale - 1) < 0.01)
            return;

        await Task.WhenAll(
            card.FadeTo(1, 180, Easing.CubicOut),
            card.TranslateTo(0, 0, 240, Easing.CubicOut),
            card.ScaleTo(1, 260, Easing.SpringOut)
        );
    }

    private async void SwipeBubble_Loaded(object sender, EventArgs e)
    {
        if (sender is not Frame bubble)
            return;

        if (Math.Abs(bubble.Scale - 1) < 0.01)
            return;

        await bubble.ScaleTo(1, 220, Easing.SpringOut);
    }

    private async Task BubbleTapAnimation(Frame bubble)
    {
        await bubble.ScaleTo(0.88, 70, Easing.CubicIn);
        await bubble.ScaleTo(1.0, 140, Easing.SpringOut);
    }
}