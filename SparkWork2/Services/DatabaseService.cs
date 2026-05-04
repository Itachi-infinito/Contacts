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

        await RunMigrations();
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await Init();
        return _database!;
    }

    private async Task RunMigrations()
    {
        if (_database == null)
            return;

        await AddColumnIfMissing("JobOffer", "Address", "TEXT DEFAULT ''");
        await AddColumnIfMissing("JobOffer", "Latitude", "REAL DEFAULT 0");
        await AddColumnIfMissing("JobOffer", "Longitude", "REAL DEFAULT 0");
        await AddColumnIfMissing("JobOffer", "SalaryMin", "INTEGER DEFAULT 0");
        await AddColumnIfMissing("JobOffer", "SalaryMax", "INTEGER DEFAULT 0");
        await AddColumnIfMissing("JobOffer", "RequiredSkills", "TEXT DEFAULT ''");
        await AddColumnIfMissing("JobOffer", "NiceToHaveSkills", "TEXT DEFAULT ''");
        await AddColumnIfMissing("JobOffer", "RemoteMode", "TEXT DEFAULT ''");
        await AddColumnIfMissing("JobOffer", "Level", "TEXT DEFAULT ''");

        await AddColumnIfMissing("CandidateProfile", "Skills", "TEXT DEFAULT ''");
        await AddColumnIfMissing("CandidateProfile", "DesiredContractType", "TEXT DEFAULT ''");
        await AddColumnIfMissing("CandidateProfile", "ExperienceLevel", "TEXT DEFAULT ''");
        await AddColumnIfMissing("CandidateProfile", "DesiredSalaryMin", "INTEGER DEFAULT 0");
        await AddColumnIfMissing("CandidateProfile", "DesiredSalaryMax", "INTEGER DEFAULT 0");
        await AddColumnIfMissing("CandidateProfile", "MaxDistanceKm", "INTEGER DEFAULT 25");
        await AddColumnIfMissing("CandidateProfile", "Latitude", "REAL DEFAULT 0");
        await AddColumnIfMissing("CandidateProfile", "Longitude", "REAL DEFAULT 0");

        await AddColumnIfMissing("CandidateProfile", "ExperienceTitle1", "TEXT DEFAULT ''");
        await AddColumnIfMissing("CandidateProfile", "ExperienceCompany1", "TEXT DEFAULT ''");
        await AddColumnIfMissing("CandidateProfile", "ExperiencePeriod1", "TEXT DEFAULT ''");

        await AddColumnIfMissing("CandidateProfile", "ExperienceTitle2", "TEXT DEFAULT ''");
        await AddColumnIfMissing("CandidateProfile", "ExperienceCompany2", "TEXT DEFAULT ''");
        await AddColumnIfMissing("CandidateProfile", "ExperiencePeriod2", "TEXT DEFAULT ''");

    }

    private async Task AddColumnIfMissing(string tableName, string columnName, string columnDefinition)
    {
        if (_database == null)
            return;

        var columns = await _database.QueryAsync<TableColumnInfo>($"PRAGMA table_info({tableName})");

        bool exists = columns.Any(column =>
            string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));

        if (exists)
            return;

        await _database.ExecuteAsync(
            $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
    }

    private class TableColumnInfo
    {
        public string Name { get; set; } = string.Empty;
    }
}
