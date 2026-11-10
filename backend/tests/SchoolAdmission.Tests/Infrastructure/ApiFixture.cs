using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Tests.Infrastructure;

/// <summary>
/// Shared by all tests: one real PostgreSQL database and two independent API instances
/// (like two backend processes) that share it.
///
/// Database source:
///  - TEST_DB_CONNECTION env var set → a fresh, uniquely named database on that server
///    (used when running the tests inside docker compose); dropped afterwards.
///  - otherwise → a throwaway PostgreSQL container via Testcontainers (needs Docker).
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dropDatabaseOnDispose;

    public string ConnectionString { get; private set; } = null!;

    /// <summary>Fake clock shared by both API instances; tests move it forward to expire holds.</summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public SchoolAdmissionFactory Api { get; private set; } = null!;
    public SchoolAdmissionFactory SecondApi { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION");

        string baseConnectionString;
        if (!string.IsNullOrWhiteSpace(external))
        {
            var csb = new NpgsqlConnectionStringBuilder(external)
            {
                Database = $"school_admission_test_{Guid.NewGuid():N}"
            };

            // Create it up front; otherwise EF logs a scary "database does not exist" error
            // before creating it itself during migration.
            await using (var admin = new NpgsqlConnection(external))
            {
                await admin.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{csb.Database}\"", admin);
                await create.ExecuteNonQueryAsync();
            }

            baseConnectionString = csb.ConnectionString;
            _dropDatabaseOnDispose = true;
        }
        else
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithCommand("-c", "max_connections=200")
                .Build();
            await _container.StartAsync();
            baseConnectionString = _container.GetConnectionString();
        }

        // Same pool cap as production so the concurrency tests exercise realistic queuing.
        ConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString) { MaxPoolSize = 40 }.ConnectionString;

        // Creating the first client starts the app, which creates the database and runs migrations.
        // Start instances one after another so they don't race on migrations.
        Api = new SchoolAdmissionFactory(ConnectionString, Clock);
        Api.CreateClient().Dispose();

        SecondApi = new SchoolAdmissionFactory(ConnectionString, Clock);
        SecondApi.CreateClient().Dispose();
    }

    public async Task DisposeAsync()
    {
        if (_dropDatabaseOnDispose)
        {
            await using var scope = Api.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        await SecondApi.DisposeAsync();
        await Api.DisposeAsync();

        if (_container is not null)
            await _container.DisposeAsync();
    }

    /// <summary>Counts HELD (not yet expired) + CONFIRMED rows straight from the database.</summary>
    public async Task<int> CountOccupiedSeatsInDbAsync(Guid classId)
    {
        await using var scope = Api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = Clock.GetUtcNow();

        return await db.Holds.CountAsync(h => h.ClassId == classId &&
            ((h.Status == HoldStatus.Held && h.ExpiresAt > now) || h.Status == HoldStatus.Confirmed));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    // Every test shares one database and fake clock, so they run one at a time in this collection.
    // Each test creates its own classes and student ids, so tests never see each other's data.
    public const string Name = "api";
}
