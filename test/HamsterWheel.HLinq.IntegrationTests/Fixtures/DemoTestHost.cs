using HamsterWheel.HLinq.Pipeline.Tokenizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.IntegrationTests.Fixtures;

internal class DemoTestHost(IConfiguration configuration) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(s => s.AddSingleton<GrammarValidator>());
        builder.UseConfiguration(configuration);
    }
}