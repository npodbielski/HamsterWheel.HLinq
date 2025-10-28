using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

[Collection(nameof(IntegrationCollectionDefinition))]
public class Client(DemoFixture fixture)
{
    [Fact]
    public async Task WhenClientConfiguredWithCustomJsonOptions_ThenUsesThoseOptionsInsteadForDeserialization()
    {
        //arrange
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory",
            q => q.For<Superhero>(JsonSerializerOptions.Default).Take(1));

        //assert
        response.First().Look.Should().BeNull();
    }
}