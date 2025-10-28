using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class MemoryDataTests
{
    [Fact]
    public async Task WhenEmptySkip_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory?skip[]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public async Task WhenSkipWithValue_ThenReturnsCorrectData(int skip)
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Skip(skip));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Skip(skip));
    }
}