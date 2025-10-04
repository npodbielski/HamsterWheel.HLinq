using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenEmptyTake_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?take[]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenNoTake_ThenReturnsAtMost1000Items()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>());

        //assert
        response.Length.Should().Be(HLinqOptions.DefaultMaxTakeRecords);
    }

    [Fact]
    public async Task WhenUserTriesToTakeToMuch_ThenLimitsTo1000Items()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Take(1000_000));

        //assert
        response.Length.Should().Be(HLinqOptions.DefaultMaxTakeRecords);
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
    public async Task WhenTakeWithValue_ThenReturnsCorrectData(int take)
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Take(take));

        //assert
        response.Should().BeEquivalentTo(Persons.Take(take));
    }
}