using FluentAssertions;
using HamsterWheel.HLinq.Client;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class MemoryDataTests
{
    [Fact]
    public async Task WhenCount_ThenReturnsCorrectCount()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Count());

        //assert
        response.Should().Be(Superhero.Superheroes.Length);
    }
    
    [Theory]
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
    public async Task WhenCountAfterSkip_ThenReturnsCorrectCount(int skip)
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Skip(skip).Count());

        //assert
        response.Should().Be(Superhero.Superheroes.Length - skip);
    }
}