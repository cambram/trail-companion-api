using Template.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Template.Infrastructure.IntegrationTests;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string Image = "postgres:17";

    private static readonly PostgreSqlContainer Container = new PostgreSqlBuilder(Image)
        .Build();

    private static readonly SemaphoreSlim StartGate = new(1, 1);
    private static bool _containerStarted;

    private string _connectionString = string.Empty;

    public string ConnectionString => _connectionString;

    public async Task InitializeAsync()
    {
        await EnsureContainerStartedAsync();

        _connectionString = await CreateDatabaseAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task EnsureContainerStartedAsync()
    {
        if (_containerStarted)
        {
            return;
        }

        await StartGate.WaitAsync();

        try
        {
            if (!_containerStarted)
            {
                await Container.StartAsync();
                _containerStarted = true;
            }
        }
        finally
        {
            StartGate.Release();
        }
    }

    private static async Task<string> CreateDatabaseAsync()
    {
        var databaseName = $"template_test_{Guid.NewGuid():N}";
        var adminConnectionString = Container.GetConnectionString();

        await using (var connection = new NpgsqlConnection(adminConnectionString))
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName
        }.ConnectionString;
    }
}
