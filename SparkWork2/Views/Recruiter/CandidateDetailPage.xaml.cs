using SparkWork2.Repositories;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Recruiter;

[QueryProperty(nameof(CandidateId), "candidateId")]
public partial class CandidateDetailPage : ContentPage
{
    private readonly UserRepository _userRepository;
    private readonly CandidateProfileRepository _candidateProfileRepository;

    private int currentCandidateId;
    private string currentCandidateName = string.Empty;

    public CandidateDetailPage(
        UserRepository userRepository,
        CandidateProfileRepository candidateProfileRepository)
    {
        InitializeComponent();
        _userRepository = userRepository;
        _candidateProfileRepository = candidateProfileRepository;
    }

    public string CandidateId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                currentCandidateId = id;
                LoadCandidate(id);
            }
        }
    }

    private async void LoadCandidate(int candidateId)
    {
        var user = await _userRepository.GetUserByIdAsync(candidateId);

        if (user == null)
        {
            await DisplayAlert("Indisponible", "Ce compte n'existe plus.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        var allCandidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        var candidate = allCandidates.FirstOrDefault(x => x.CandidateId == candidateId);

        if (candidate == null)
        {
            await DisplayAlert("Indisponible", "Ce profil n'existe plus.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        var fullName = string.IsNullOrWhiteSpace(candidate.FullName)
            ? user.FullName
            : candidate.FullName;

        currentCandidateName = fullName;

        lblFullName.Text = fullName;
        lblTitle.Text = string.IsNullOrWhiteSpace(candidate.Title)
            ? "Titre non renseigné"
            : candidate.Title;
        lblLocation.Text = string.IsNullOrWhiteSpace(candidate.Location)
            ? "Localisation non renseignée"
            : candidate.Location;
        lblAbout.Text = string.IsNullOrWhiteSpace(candidate.About)
            ? "Ce candidat n'a pas encore ajouté de description."
            : candidate.About;
        lblEmail.Text = string.IsNullOrWhiteSpace(candidate.Email)
            ? user.Email
            : candidate.Email;

        lblCandidateInitials.Text = BuildInitials(fullName);

        if (!string.IsNullOrWhiteSpace(candidate.PhotoPath) && File.Exists(candidate.PhotoPath))
        {
            imgCandidatePhoto.Source = ImageSource.FromFile(candidate.PhotoPath);
            candidatePhotoFrame.IsVisible = true;
            candidatePlaceholderFrame.IsVisible = false;
        }
        else
        {
            imgCandidatePhoto.Source = null;
            candidatePhotoFrame.IsVisible = false;
            candidatePlaceholderFrame.IsVisible = true;
        }
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async void Contact_Clicked(object sender, EventArgs e)
    {
        if (currentCandidateId <= 0)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(ConversationDetailPage)}" +
            $"?participantId={currentCandidateId}" +
            $"&participantName={Uri.EscapeDataString(currentCandidateName)}");
    }

    private async void Discover_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterSwipePage)}");
    }

    private async void Messages_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void Stats_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RecruiterMatchesPage)}");
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
