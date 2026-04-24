using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class UserRepository
{
    private readonly DatabaseService _databaseService;

    public UserRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var db = await _databaseService.GetConnectionAsync();
        return await db.Table<User>().ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        if (userId <= 0)
            return null;

        var db = await _databaseService.GetConnectionAsync();
        return await db.Table<User>()
                       .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = NormalizeEmail(email);

        var db = await _databaseService.GetConnectionAsync();
        return await db.Table<User>()
                       .FirstOrDefaultAsync(x => x.Email == normalizedEmail);
    }

    public async Task<int> AddUserAsync(User user)
    {
        if (user == null)
            return 0;

        user.Email = NormalizeEmail(user.Email);

        var db = await _databaseService.GetConnectionAsync();
        return await db.InsertAsync(user);
    }

    public async Task<int> UpdateUserAsync(User user)
    {
        if (user == null || user.UserId <= 0)
            return 0;

        user.Email = NormalizeEmail(user.Email);

        var db = await _databaseService.GetConnectionAsync();
        return await db.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        if (userId <= 0)
            return false;

        var db = await _databaseService.GetConnectionAsync();

        var user = await db.Table<User>()
                           .FirstOrDefaultAsync(x => x.UserId == userId);

        if (user == null)
            return false;

        var result = await db.DeleteAsync(user);
        return result > 0;
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}