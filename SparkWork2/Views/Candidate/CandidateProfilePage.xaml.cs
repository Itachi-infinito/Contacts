using SparkWork2.Models;
using SparkWork2.Repositories;
using SparkWork2.Services;
using SparkWork2.Views.Shared;

namespace SparkWork2.Views.Candidate;

public partial class CandidateProfilePage : ContentPage
{
    private readonly CandidateProfileRepository _candidateProfileRepository;
    private readonly SessionService _sessionService;

    public CandidateProfilePage(
        CandidateProfileRepository candidateProfileRepository,
        SessionService sessionService)
    {
        InitializeComponent();
        _candidateProfileRepository = candidateProfileRepository;
        _sessionService = sessionService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfile();
    }

    private async Task LoadProfile()
    {
        if (!_sessionService.IsLoggedIn)
            return;

        var profile = await _candidateProfileRepository.GetCandidateProfileAsync(
            _sessionService.CurrentUserId,
            _sessionService.CurrentUserName,
            _sessionService.CurrentUserEmail);

        var fullName = string.IsNullOrWhiteSpace(profile.FullName)
            ? _sessionService.CurrentUserName
            : profile.FullName;

        var title = string.IsNullOrWhiteSpace(profile.Title)
            ? "Junior vendeur"
            : profile.Title;

        var location = string.IsNullOrWhiteSpace(profile.Location)
            ? "Localisation non renseignée"
            : profile.Location;

        lblFullName.Text = fullName;
        lblTitleLine.Text = $"{title} · Horeca";

        lblDistanceLine.Text = profile.MaxDistanceKm > 0
            ? $"{location} · {profile.MaxDistanceKm} km des offres"
            : location;

        lblDesiredContractType.Text = string.IsNullOrWhiteSpace(profile.DesiredContractType)
            ? "Non renseigné"
            : profile.DesiredContractType;

        lblDesiredSalary.Text = GetSalaryDisplay(profile);

        lblMaxDistance.Text = profile.MaxDistanceKm > 0
            ? $"{profile.MaxDistanceKm} km"
            : "Non renseignée";

        lblWorkMode.Text = "Flexible";
        lblAvailability.Text = "Immédiate";

        var initials = BuildInitials(fullName);
        lblHeaderInitials.Text = initials;
        lblProfileInitials.Text = initials;

        SetProfilePhoto(profile.PhotoPath);
        RenderSkills(profile.Skills);
        RenderExperiences(profile);
        UpdateCompletion(profile);
        UpdateStats();
    }

    private void SetProfilePhoto(string? photoPath)
    {
        if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
        {
            imgProfilePhoto.Source = ImageSource.FromFile(photoPath);
            profilePhotoFrame.IsVisible = true;
            profilePlaceholderFrame.IsVisible = false;
        }
        else
        {
            imgProfilePhoto.Source = null;
            profilePhotoFrame.IsVisible = false;
            profilePlaceholderFrame.IsVisible = true;
        }
    }

    private void RenderSkills(string? skillsText)
    {
        skillsLayout.Children.Clear();

        var skills = (skillsText ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Take(8)
            .ToList();

        lblNoSkills.IsVisible = skills.Count == 0;

        for (int i = 0; i < skills.Count; i++)
        {
            skillsLayout.Children.Add(CreateSkillChip(skills[i], GetSkillLevel(i)));
        }
    }

    private Frame CreateSkillChip(string skill, int level)
    {
        var content = new HorizontalStackLayout
        {
            Spacing = 7,
            VerticalOptions = LayoutOptions.Center
        };

        content.Children.Add(new Label
        {
            Text = skill,
            FontSize = 14,
            TextColor = Color.FromArgb("#4B2FBF"),
            VerticalTextAlignment = TextAlignment.Center
        });

        var dots = new HorizontalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center
        };

        for (int i = 1; i <= 4; i++)
        {
            dots.Children.Add(new BoxView
            {
                WidthRequest = 6,
                HeightRequest = 6,
                CornerRadius = 3,
                Color = i <= level
                    ? Color.FromArgb("#7C4DFF")
                    : Color.FromArgb("#DED8F5"),
                VerticalOptions = LayoutOptions.Center
            });
        }

        content.Children.Add(dots);

        return new Frame
        {
            BackgroundColor = Color.FromArgb("#F7F4FF"),
            BorderColor = Color.FromArgb("#CDBDFF"),
            CornerRadius = 14,
            Padding = new Thickness(12, 5),
            HasShadow = false,
            Margin = new Thickness(0, 0, 8, 8),
            Content = content
        };
    }

    private static int GetSkillLevel(int index)
    {
        return index switch
        {
            0 => 3,
            1 => 2,
            2 => 3,
            _ => 2
        };
    }

    private void RenderExperiences(CandidateProfile profile)
    {
        experiencesLayout.Children.Clear();

        var experiences = new List<(string Title, string Company, string Period)>
        {
            (profile.ExperienceTitle1, profile.ExperienceCompany1, profile.ExperiencePeriod1),
            (profile.ExperienceTitle2, profile.ExperienceCompany2, profile.ExperiencePeriod2)
        }
        .Where(x =>
            !string.IsNullOrWhiteSpace(x.Title) ||
            !string.IsNullOrWhiteSpace(x.Company) ||
            !string.IsNullOrWhiteSpace(x.Period))
        .ToList();

        lblNoExperiences.IsVisible = experiences.Count == 0;

        foreach (var experience in experiences)
        {
            experiencesLayout.Children.Add(CreateExperienceItem(
                string.IsNullOrWhiteSpace(experience.Title) ? "Poste renseigné" : experience.Title,
                string.IsNullOrWhiteSpace(experience.Company) ? "Entreprise non renseignée" : experience.Company,
                string.IsNullOrWhiteSpace(experience.Period) ? "Période non renseignée" : experience.Period));
        }
    }

    private View CreateExperienceItem(string title, string company, string period)
    {
        var dot = new Frame
        {
            WidthRequest = 18,
            HeightRequest = 18,
            CornerRadius = 9,
            Padding = 0,
            HasShadow = false,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 3, 0, 0),
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
            {
                new GradientStop(Color.FromArgb("#7C4DFF"), 0),
                new GradientStop(Color.FromArgb("#D34C88"), 1)
            }
            }
        };

        var content = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
        {
            new Label
            {
                Text = title,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            },
            new Label
            {
                Text = company,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#7C4DFF")
            },
            new Label
            {
                Text = period,
                FontSize = 12,
                TextColor = Color.FromArgb("#8B86A3")
            },
            new Label
            {
                Text = "Expérience ajoutée au profil candidat.",
                FontSize = 12,
                TextColor = Color.FromArgb("#5F5A75")
            }
        }
        };

        Grid.SetColumn(content, 1);

        var grid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star }
        },
            ColumnSpacing = 12
        };

        grid.Children.Add(dot);
        grid.Children.Add(content);

        return grid;
    }


    private void UpdateCompletion(CandidateProfile profile)
    {
        bool hasPhoto = true;
        bool hasPost = !string.IsNullOrWhiteSpace(profile.Title);
        bool hasSkills = !string.IsNullOrWhiteSpace(profile.Skills);
        bool hasExperiences =
            !string.IsNullOrWhiteSpace(profile.ExperienceTitle1) ||
            !string.IsNullOrWhiteSpace(profile.ExperienceCompany1) ||
            !string.IsNullOrWhiteSpace(profile.ExperienceTitle2) ||
            !string.IsNullOrWhiteSpace(profile.ExperienceCompany2);
        bool hasSalary = profile.DesiredSalaryMin > 0 || profile.DesiredSalaryMax > 0;
        bool hasCv = false;

        int percent = 0;
        if (hasPhoto) percent += 15;
        if (hasPost) percent += 25;
        if (hasSkills) percent += 25;
        if (hasExperiences) percent += 15;
        if (hasSalary) percent += 10;
        if (hasCv) percent += 10;

        lblCompletionPercent.Text = $"{percent}%";
        profileCompletionProgress.Progress = percent / 100.0;

        SetStepChip(photoStepChip, lblPhotoStep, "Photo", hasPhoto);
        SetStepChip(postStepChip, lblPostStep, "Poste", hasPost);
        SetStepChip(skillsStepChip, lblSkillsStep, "Compétences", hasSkills);
        SetStepChip(experiencesStepChip, lblExperiencesStep, "Expériences", hasExperiences);
        SetStepChip(salaryStepChip, lblSalaryStep, "Salaire", hasSalary);
        SetStepChip(cvStepChip, lblCvStep, "CV", hasCv);
    }

    private static void SetStepChip(Frame chip, Label label, string text, bool isDone)
    {
        chip.BackgroundColor = isDone
            ? Color.FromArgb("#DCFCE7")
            : Color.FromArgb("#F0EAFE");

        label.Text = isDone ? $"✓ {text}" : $"+ {text}";
        label.TextColor = isDone
            ? Color.FromArgb("#047857")
            : Color.FromArgb("#7C4DFF");
    }

    private void UpdateStats()
    {
        lblProfileViewsCount.Text = "47";
        lblMatchesCount.Text = "3";
        lblConversationsCount.Text = "2";
        lblOffersSeenCount.Text = "8";
    }

    private static string GetSalaryDisplay(CandidateProfile profile)
    {
        if (profile.DesiredSalaryMin > 0 && profile.DesiredSalaryMax > 0)
            return $"{profile.DesiredSalaryMin} - {profile.DesiredSalaryMax} €";

        if (profile.DesiredSalaryMin > 0)
            return $"Dès {profile.DesiredSalaryMin} €";

        if (profile.DesiredSalaryMax > 0)
            return $"Jusqu'à {profile.DesiredSalaryMax} €";

        return "Non renseigné";
    }

    private static string BuildInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "ME";

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async void Home_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(CandidateHomePage)}");
    }

    private async void Discover_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CandidateSwipePage));
    }

    private async void Messages_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(MessagesPage)}");
    }

    private async void EditProfile_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditCandidateProfilePage));
    }

    private async void Settings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
