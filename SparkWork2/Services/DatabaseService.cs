using SQLite;
using SparkWork2.Models;

namespace SparkWork2.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    public async Task Init()
    {
        if (_database != null)
            return;

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "sparkwork2.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<User>();
        await _database.CreateTableAsync<CandidateProfile>();
        await _database.CreateTableAsync<RecruiterProfile>();
        await _database.CreateTableAsync<JobOffer>();
        await _database.CreateTableAsync<Match>();
        await _database.CreateTableAsync<Message>();
        await _database.CreateTableAsync<CandidateJobLike>();
        await _database.CreateTableAsync<RecruiterCandidateLike>();
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await Init();
        return _database!;
    }
}