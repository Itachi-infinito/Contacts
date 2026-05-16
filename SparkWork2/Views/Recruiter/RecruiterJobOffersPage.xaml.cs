using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterJobOffersPage : ContentPage
{
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly SessionService _sessionService;
    private readonly MatchRepository _matchRepository;


    public RecruiterJobOffersPage(
    JobOfferRepository jobOfferRepository,
    SessionService sessionService,
    MatchRepository matchRepository)
    {
        InitializeComponent();

        _jobOfferRepository = jobOfferRepository;
        _sessionService = sessionService;
        _matchRepository = matchRepository;
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

        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(
            _sessionService.CurrentUserId);

        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);

        var offerCards = offers
            .Select(offer => new JobOfferCardItem(offer))
            .ToList();

        jobOffersCollection.ItemsSource = null;
        jobOffersCollection.ItemsSource = offerCards;

        bool hasOffers = offerCards.Any();

        offersContent.IsVisible = hasOffers;
        emptyStateLayout.IsVisible = !hasOffers;

        lblOfferCount.Text = hasOffers
            ? offers.Count == 1 ? "1 offre publiée" : $"{offers.Count} offres publiées"
            : "Aucune offre publiée";

        lblActiveOffersCount.Text = offerCards.Count(x => !x.IsPaused).ToString();
        lblCandidatesSeenCount.Text = "0";
        lblTotalMatchesCount.Text = matches.Count.ToString();
    }


    private async void CreateOffer_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));

    }

    private async void Edit_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is JobOfferCardItem selected)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(EditJobOfferPage)}?id={selected.Offer.JobOfferId}");
        }
    }

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is JobOfferCardItem selected)
        {
            await DeleteJobOfferAsync(selected.Offer);
        }
    }


    private async void EditBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not JobOfferCardItem selected)
            return;

        await BubbleTapAnimation(frame);

        await Shell.Current.GoToAsync(
            $"{nameof(EditJobOfferPage)}?id={selected.Offer.JobOfferId}");
    }

    private async void DeleteBubble_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not JobOfferCardItem selected)
            return;

        await BubbleTapAnimation(frame);
        await DeleteJobOfferAsync(selected.Offer);
    }


    private async Task DeleteJobOfferAsync(JobOffer selectedJobOffer)
    {
        var popup = new DeleteConfirmationPopup(
            $"Supprimer l'offre \"{selectedJobOffer.Title}\" ?");

        await Navigation.PushModalAsync(popup);

        bool confirmed = await popup.CompletionSource.Task;
        if (!confirmed)
            return;

        await _jobOfferRepository.DeleteJobOfferAsync(selectedJobOffer.JobOfferId);
        await LoadJobOffers();
    }

    private void Menu_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));

    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterMatchesPage)}");
    }

    private async void Profile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }


    private async void JobOfferCard_Loaded(object sender, EventArgs e)
    {
        if (sender is not Frame card)
            return;

        if (card.Opacity >= 0.99 &&
            Math.Abs(card.TranslationY) < 0.1 &&
            Math.Abs(card.Scale - 1) < 0.01)
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
    private async void Profile_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterProfilePage)}");
    }

    private async Task BubbleTapAnimation(Frame bubble)
    {
        await bubble.ScaleTo(0.88, 70, Easing.CubicIn);
        await bubble.ScaleTo(1.0, 140, Easing.SpringOut);
    }
    private async void Home_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterHomePage)}");
    }
    private async void Discover_Button_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));

    }

    private async void Messages_Button_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private sealed class JobOfferCardItem
    {
        public JobOffer Offer { get; }

        public string Title => Offer.Title;
        public string CompanyName => Offer.CompanyName;
        public string Location => Offer.Location;
        public string ContractType => Offer.ContractType;
        public string Level => Offer.Level;
        public string RemoteMode => Offer.RemoteMode;
        public string RequiredSkills => Offer.RequiredSkills;

        public bool HasLevel => !string.IsNullOrWhiteSpace(Level);
        public bool HasRemoteMode => !string.IsNullOrWhiteSpace(RemoteMode);
        public bool HasRequiredSkills => !string.IsNullOrWhiteSpace(RequiredSkills);

        public bool IsPaused { get; }
        public string StatusText => IsPaused ? "Ⅱ En pause" : "• Actif";
        public string StatusBackground => IsPaused ? "#FEF3C7" : "#DCFCE7";
        public string StatusColor => IsPaused ? "#92400E" : "#166534";
        public double CardOpacity => IsPaused ? 0.72 : 1;

        public string PrimaryActionText => IsPaused ? "Réactiver" : "Modifier";
        public string PrimaryActionIcon => IsPaused ? "▷" : "✎";

        public string ViewsCount => "0";
        public string MatchesCount => "0";
        public string RetainedCount => "0";

        public bool HasSalary => Offer.SalaryMin > 0 || Offer.SalaryMax > 0;

        public string SalaryDisplay
        {
            get
            {
                if (Offer.SalaryMin > 0 && Offer.SalaryMax > 0)
                    return $"{Offer.SalaryMin} — {Offer.SalaryMax} €";

                if (Offer.SalaryMin > 0)
                    return $"À partir de {Offer.SalaryMin} €";

                if (Offer.SalaryMax > 0)
                    return $"Jusqu'à {Offer.SalaryMax} €";

                return string.Empty;
            }
        }

        public List<string> RequiredSkillItems =>
            string.IsNullOrWhiteSpace(Offer.RequiredSkills)
                ? new List<string>()
                : Offer.RequiredSkills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(4)
                    .ToList();

        public JobOfferCardItem(JobOffer offer)
        {
            Offer = offer;
            IsPaused = false;
        }
    }


}
