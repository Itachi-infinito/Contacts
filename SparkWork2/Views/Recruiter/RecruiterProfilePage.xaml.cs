using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

public partial class RecruiterProfilePage : ContentPage
{
    private readonly RecruiterProfileRepository _recruiterProfileRepository;
    private readonly SessionService _sessionService;
    private readonly MatchRepository _matchRepository;
    private readonly JobOfferRepository _jobOfferRepository;
    private readonly MessageRepository _messageRepository;


    public RecruiterProfilePage(
    RecruiterProfileRepository recruiterProfileRepository,
    SessionService sessionService,
    MatchRepository matchRepository,
    JobOfferRepository jobOfferRepository,
    MessageRepository messageRepository)
    {
        InitializeComponent();

        _recruiterProfileRepository = recruiterProfileRepository;
        _sessionService = sessionService;
        _matchRepository = matchRepository;
        _jobOfferRepository = jobOfferRepository;
        _messageRepository = messageRepository;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadProfile();
        await LoadProfileStats();
    }
    private async Task LoadProfileStats()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var matches = await _matchRepository.GetMatchesAsync(_sessionService.CurrentUserId);
        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);
        var conversations = await _messageRepository.GetConversationsAsync(_sessionService.CurrentUserId);

        lblProfileViewsCount.Text = "0";
        lblMatchesCount.Text = matches.Count.ToString();
        lblOffersCount.Text = offers.Count.ToString();
        lblConversationsCount.Text = conversations.Count().ToString();
    }



    private async Task LoadProfile()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var profile = await _recruiterProfileRepository.GetRecruiterProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        var companyName = string.IsNullOrWhiteSpace(profile.CompanyName)
            ? _sessionService.CurrentUserName
            : profile.CompanyName;

        var sector = string.IsNullOrWhiteSpace(profile.Sector)
            ? "Secteur non renseigné"
            : profile.Sector;

        var location = string.IsNullOrWhiteSpace(profile.Location)
            ? "Localisation non renseignée"
            : profile.Location;

        var contactEmail = string.IsNullOrWhiteSpace(profile.ContactEmail)
            ? _sessionService.CurrentUserEmail
            : profile.ContactEmail;

        var description = string.IsNullOrWhiteSpace(profile.Description)
            ? "Ajoute une description pour présenter ton entreprise aux candidats."
            : profile.Description;

        lblCompanyName.Text = companyName;
        lblSector.Text = sector;
        lblLocation.Text = location;
        lblContactEmail.Text = contactEmail;
        lblDescription.Text = description;

        var initials = BuildInitials(companyName);
        lblHeaderInitials.Text = initials;
        lblCompanyInitials.Text = initials;

        if (!string.IsNullOrWhiteSpace(profile.CompanyPhotoPath) && File.Exists(profile.CompanyPhotoPath))
        {
            imgCompanyPhoto.Source = ImageSource.FromFile(profile.CompanyPhotoPath);
            companyPhotoFrame.IsVisible = true;
            companyPlaceholderFrame.IsVisible = false;
        }
        else
        {
            imgCompanyPhoto.Source = null;
            companyPhotoFrame.IsVisible = false;
            companyPlaceholderFrame.IsVisible = true;
        }
        int completed = 0;
        int total = 5;

        bool hasPhoto = !string.IsNullOrWhiteSpace(profile.CompanyPhotoPath) && File.Exists(profile.CompanyPhotoPath);
        bool hasSector = !string.IsNullOrWhiteSpace(profile.Sector);
        bool hasLocation = !string.IsNullOrWhiteSpace(profile.Location);
        bool hasDescription = !string.IsNullOrWhiteSpace(profile.Description);

        if (hasPhoto) completed++;
        if (hasSector) completed++;
        if (hasLocation) completed++;
        if (hasDescription) completed++;

        var offers = await _jobOfferRepository.GetJobOffersByRecruiterAsync(_sessionService.CurrentUserId);
        if (offers.Any()) completed++;

        int percent = (int)Math.Round(completed * 100.0 / total);

        lblProfileCompletionPercent.Text = $"{percent}%";
        profileCompletionProgress.Progress = percent / 100.0;

        lblPhotoChip.Text = hasPhoto ? "✓ Photo" : "+ Photo";
        lblSectorChip.Text = hasSector ? "✓ Secteur" : "+ Secteur";
        lblLocationChip.Text = hasLocation ? "✓ Localisation" : "+ Localisation";
        lblDescriptionChip.Text = hasDescription ? "✓ Description" : "+ Description";
        lblOffersChip.Text = offers.Any() ? "✓ Offres" : "+ Offres";

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

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditRecruiterProfilePage));
    }

    private async void Offers_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterJobOffersPage));
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
    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
    private async void Home_Nav_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterHomePage)}");
    }

    private async void Discover_Nav_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecruiterSwipePage));
    }

    private async void AddOffer_Nav_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddJobOfferPage));
    }

    private async void Messages_Nav_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MessagesPage));
    }
    private async void EditProfile_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditRecruiterProfilePage));
    }

    

    private async void ShareProfile_Tapped(object sender, TappedEventArgs e)
    {
        await DisplayAlert("Partager", "Le partage du profil arrive bientôt.", "OK");
    }


}
