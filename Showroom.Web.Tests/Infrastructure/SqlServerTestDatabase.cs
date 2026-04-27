using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Showroom.Web.Tests.Infrastructure;

internal sealed class SqlServerTestDatabase : IAsyncDisposable
{
    private SqlServerTestDatabase(string databaseName, string connectionString, string masterConnectionString)
    {
        DatabaseName = databaseName;
        ConnectionString = connectionString;
        MasterConnectionString = masterConnectionString;
    }

    public string ConnectionString { get; }

    public string DatabaseName { get; }

    private string MasterConnectionString { get; }

    public static async Task<SqlServerTestDatabase> CreateAsync()
    {
        var repoRoot = GetRepositoryRoot();
        var baseConnectionString = ResolveBaseConnectionString(repoRoot);
        var databaseName = $"AutoCarShowroomDb_Tests_{Guid.NewGuid():N}";

        var masterBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = "master"
        };
        var databaseBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = databaseName
        };

        var setupScriptPath = Path.Combine(repoRoot, "database", "setup.sql");
        var setupScript = await File.ReadAllTextAsync(setupScriptPath);
        setupScript = setupScript
            .Replace("AutoCarShowroomDb", databaseName, StringComparison.Ordinal)
            .Replace("AutoCarShowRoomDb", databaseName, StringComparison.Ordinal);

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();

        foreach (var batch in SplitSqlBatches(setupScript))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync();
        }

        return new SqlServerTestDatabase(databaseName, databaseBuilder.ConnectionString, masterBuilder.ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        var dropSql = $"""
            IF DB_ID(N'{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END;
            """;

        await using var command = new SqlCommand(dropSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
        => Regex.Split(script, @"^\s*GO\s*$(?:\r?\n)?", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static string ResolveBaseConnectionString(string repoRoot)
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable("SHOWROOM_TEST_SQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(repoRoot)
            .AddJsonFile(Path.Combine("Showroom.Web", "appsettings.json"), optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("ShowroomDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ShowroomDb connection string is missing for SQL integration tests.");
        }

        return connectionString;
    }

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
