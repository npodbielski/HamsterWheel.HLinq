using HamsterWheel.HLinq.Demo.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HamsterWheel.HLinq.IntegrationTests.Fixtures;

public class PostGreSqlFixture : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly PostgreSqlContainer? _dbContainer;
    public DemoContext DemoContext { get; private set; } = null!;

    public PostGreSqlFixture(DemoFixture fixture, string connectionString)
    {
        _connectionString = connectionString;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (!fixture.IsInCi)
        {
            _dbContainer = new PostgreSqlBuilder()
                .WithImage("postgres:latest")
                .WithPortBinding(int.Parse(builder["Port"].ToString()!), 5432)
                .WithPassword(builder["Password"].ToString())
                .Build();
        }
    }

    public async Task InitializeAsync()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.StartAsync();
        }

        DemoContext =
            new DemoContext(new DbContextOptionsBuilder<DemoContext>().UseNpgsql(_connectionString).Options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}