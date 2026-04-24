using SparkWork2.Repositories;

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
            await DisplayAlert("Unavailable", "This account no longer exists.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        var allCandidates = await _candidateProfileRepository.GetAllCandidateProfilesAsync();
        var candidate = allCandidates.FirstOrDefault(x => x.CandidateId == candidateId);

        if (candidate == null)
        {
            await DisplayAlert("Unavailable", "This profile no longer exists.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        currentCandidateName = candidate.FullName;

        lblFullName.Text = candidate.FullName;
        lblTitle.Text = candidate.Title;
        lblLocation.Text = candidate.Location;
        lblAbout.Text = candidate.About;
        lblEmail.Text = candidate.Email;
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}