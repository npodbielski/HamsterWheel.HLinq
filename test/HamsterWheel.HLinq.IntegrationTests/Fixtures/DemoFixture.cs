using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace HamsterWheel.HLinq.IntegrationTests.Fixtures;

public class DemoFixture : IAsyncLifetime
{
    public bool IsInCi => Environment.GetEnvironmentVariable("CI_SERVER") != null;

    public string ConnectionString =>
        IsInCi
            ? $"Host=postgres;Port=5432;" +
              $"Database={Environment.GetEnvironmentVariable("POSTGRES_ENV_POSTGRES_DB")};Username=postgres;" +
              $"Password={Environment.GetEnvironmentVariable("POSTGRES_ENV_POSTGRES_PASSWORD")};Include Error Detail=true"
            : "Host=localhost;Port=55432;Database=Demo;Username=postgres;Password=outflank-outage-undoing;Include Error Detail=true";

    public PostGreSqlFixture PgSqlFixture { get; set; }

    internal DemoTestHost DemoTestHost { get; set; }

    public HttpClient Client { get; set; }

    public async Task InitializeAsync()
    {
        PgSqlFixture = new PostGreSqlFixture(this, ConnectionString);
        await PgSqlFixture.InitializeAsync();
        var configuration = new ConfigurationRoot([new MemoryConfigurationProvider(new MemoryConfigurationSource())]);
        configuration["ConnectionStrings:Demo"] = ConnectionString;
        DemoTestHost = new DemoTestHost(configuration);
        Client = DemoTestHost.CreateClient();
    }

    public async Task DisposeAsync() => await PgSqlFixture.DisposeAsync();
}