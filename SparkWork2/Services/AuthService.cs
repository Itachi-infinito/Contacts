using SparkWork2.Models;
using SparkWork2.Repositories;

namespace SparkWork2.Services;

public class AuthService
{
    private readonly UserRepository _userRepository;
    private readonly PasswordService _passwordService;

    public AuthService(UserRepository userRepository, PasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterUserAsync(
        string fullName,
        string email,
        string password,
        string role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return (false, "Le nom complet est requis.");

        if (string.IsNullOrWhiteSpace(email))
            return (false, "L'email est requis.");

        if (string.IsNullOrWhiteSpace(password))
            return (false, "Le mot de passe est requis.");

        if (string.IsNullOrWhiteSpace(role))
            return (false, "Le rôle est requis.");

        string normalizedEmail = NormalizeEmail(email);

        var existingUser = await _userRepository.GetUserByEmailAsync(normalizedEmail);
        if (existingUser != null)
            return (false, "Un compte existe déjà avec cet email.");

        var user = new User
        {
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordService.HashPassword(password),
            Role = role.Trim()
        };

        int result = await _userRepository.AddUserAsync(user);

        return result > 0
            ? (true, string.Empty)
            : (false, "Impossible de créer le compte.");
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        string normalizedEmail = NormalizeEmail(email);

        var user = await _userRepository.GetUserByEmailAsync(normalizedEmail);
        if (user == null)
            return null;

        bool isValid = _passwordService.VerifyPassword(password, user.PasswordHash);
        return isValid ? user : null;
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
