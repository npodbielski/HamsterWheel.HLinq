using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace HamsterWheel.HLinq.IntegrationTests.Fixtures;

public class DemoFixture : IAsyncLifetime
{
    public bool IsInCi => Environment.GetEnvironmentVariable("CI_SERVER") != null;

    private string ConnectionString =>
        IsInCi
            ? $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST")};Port=5432;" +
              $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB")};Username=postgres;" +
              $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")};Include Error Detail=true"
            : "Host=localhost;Port=45432;Database=Demo;Username=postgres;Password=outflank-outage-undoing;Include Error Detail=true";

    public PostGreSqlFixture PgSqlFixture { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    private DemoTestHost DemoTestHost { get; set; } = null!;

    public async Task InitializeAsync()
    {
        PgSqlFixture = new PostGreSqlFixture(this, ConnectionString);
        await PgSqlFixture.InitializeAsync();
        var configuration = new ConfigurationRoot([new MemoryConfigurationProvider(new MemoryConfigurationSource())]);
        configuration["ConnectionStrings:Demo"] = ConnectionString;
        DemoTestHost = new DemoTestHost(configuration);
        Client = DemoTestHost.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await DemoTestHost.DisposeAsync();
        await PgSqlFixture.DisposeAsync();
    }
}