using System.Security.Cryptography;
using System.Text;

namespace SparkWork2.Services;

public class PasswordService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "PBKDF2-SHA256";

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        byte[] hash = pbkdf2.GetBytes(KeySize);

        return $"{Prefix}:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (!storedHash.StartsWith($"{Prefix}:", StringComparison.Ordinal))
            return VerifyLegacySha256(password, storedHash);

        try
        {
            var parts = storedHash.Split(':');

            if (parts.Length != 4)
                return false;

            int iterations = int.Parse(parts[1]);
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);

            byte[] actualHash = pbkdf2.GetBytes(expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    public bool NeedsRehash(string storedHash)
    {
        return string.IsNullOrWhiteSpace(storedHash) ||
               !storedHash.StartsWith($"{Prefix}:{Iterations}:", StringComparison.Ordinal);
    }

    private static bool VerifyLegacySha256(string password, string storedHash)
    {
        try
        {
            byte[] expectedHash = Convert.FromBase64String(storedHash);
            byte[] actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));

            return expectedHash.Length == actualHash.Length &&
                   CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
