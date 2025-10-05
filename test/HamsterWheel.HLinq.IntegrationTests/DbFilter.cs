using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenEmptyWhere_ThenReturnsNotFilteredCollectionButWithMaxTakeApplied()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?where[]");

        //assert
        var data = await response.Content.ReadFromJsonAsync<Person[]>();
        data.Should().BeEquivalentTo(Persons.Take(HLinqOptions.DefaultMaxTakeRecords));
    }

    [Fact]
    public async Task WhenILikeUsedOnInMemoryConnection_ThenBadRequest()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?where[ilike(x.name, billy)]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenStringContains_ThenCanFilter()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().Where(x => x.FirstName.Contains("Billy")));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => x.FirstName.Contains("Billy")));
    }

    [Fact]
    public async Task WhenStringContainsIgnoreCase_ThenThrows()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?" + new HLinqClientQueryBuilderFactory().For<Person>()
            .Where(x => x.FirstName.Contains("Billy", StringComparison.OrdinalIgnoreCase)).BuildQuery());

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task WhenStringPropertyEquals_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Where(x => x.FirstName == "Billy"));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => x.FirstName == "Billy"));
    }

    [Fact]
    public async Task WhenStringPropertyEqualsAndDataIsNotInFirst1000Records_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Where(x => x.FirstName == "Montgomery"));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => x.FirstName == "Montgomery"));
    }

    [Fact]
    public async Task WhenStringPropertyNotEquals_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Where(x => x.FirstName != "Billy").Take(100));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => x.FirstName != "Billy").Take(100));
    }

    [Fact]
    public async Task WhenStringPropertyEqualsToNull_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Where(x => x.IpAddress == null));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => x.IpAddress == null));
    }

    [Theory]
    [MemberData(nameof(IntData))]
    public async Task WhenIntPropertyEquals_ThenCanFilter(int value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Where(x => x.Id == value));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => x.Id == value));
    }

    public static TheoryData<int> IntData =>
    [
        int.MinValue,
        int.MaxValue,
        -1,
        0,
        1,
        10,
        1000,
        323123,
    ];
}