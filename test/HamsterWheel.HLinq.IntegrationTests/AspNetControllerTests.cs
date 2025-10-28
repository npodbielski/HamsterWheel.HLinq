using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

[Collection(nameof(IntegrationCollectionDefinition))]
public class AspNetControllerTests(DemoFixture fixture)
{
    [Fact]
    public async Task WhenNoSpecificQuery_ThenReturnsOk()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}