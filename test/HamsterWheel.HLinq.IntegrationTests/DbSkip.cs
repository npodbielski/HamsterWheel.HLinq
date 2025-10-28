using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenEmptySkip_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?skip[]");

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
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Skip(skip).Take(100));

        //assert
        response.Should().BeEquivalentTo(Persons.Skip(skip).Take(100));
    }
}