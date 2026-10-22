using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HamsterWheel.HLinq.IntegrationTests.Fixtures;

internal class DemoTestHost(IConfiguration configuration) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseConfiguration(configuration);
    }
}