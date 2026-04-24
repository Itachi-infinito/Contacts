using SparkWork2.Models;

namespace SparkWork2.Services;

public class SessionService
{
    public User? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser is not null;

    public int CurrentUserId => CurrentUser?.UserId ?? 0;
    public string CurrentUserName => CurrentUser?.FullName ?? string.Empty;
    public string CurrentUserEmail => CurrentUser?.Email ?? string.Empty;
    public string CurrentUserRole => CurrentUser?.Role ?? string.Empty;

    public void SetSession(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        CurrentUser = user;
    }

    public void ClearSession()
    {
        CurrentUser = null;
    }
}