using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

[Collection(nameof(IntegrationCollectionDefinition))]
public partial class RandomDataTests(DemoFixture fixture)
{
    [Fact]
    public async Task WhenStringPropertyEqualsToNull_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetAsync("/demo/random?where[x.Id == \"846e7ce6-ef91-43b9-877f-98ad793beca1\"]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}