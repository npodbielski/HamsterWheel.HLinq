using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using Microsoft.EntityFrameworkCore;

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
    public async Task WhenILikeUsedOnDb_ThenCanFilter()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().Where(x => EF.Functions.ILike(x.FirstName, "Bill")));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => EF.Functions.ILike(x.FirstName, "Bill")));
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

    /// <summary>
    /// THis need to be done via query string. This is because HLinq client is unable to do ignore-case comparison
    /// This does not work in EF out of the box either, so it is fine to not be able to define such in HLinq client. At least for now.
    /// </summary>
    [Fact]
    public async Task WhenStringContainsIgnoreCase_ThenThrows()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?" + new HLinqClientQueryBuilderFactory().For<Person>()
            .Where(x => x.FirstName.Contains("Billy", StringComparison.OrdinalIgnoreCase)).BuildAndEncode());

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task WhenEFFunction_ThenCanFilter()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().Where(x => EF.Functions.ILike(x.FirstName, "Billy")));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x => EF.Functions.ILike(x.FirstName, "Billy")));
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
    public async Task WhenTwoGroupsOfCondition_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>()
                .Where(x => (x.FirstName == "Lil" && x.LastName == "Scatchar") ||
                            (x.Email.Contains("ncowle0@") && x.Email.EndsWith(".com"))));

        //assert
        var expected = Persons.Where(x => (x.FirstName == "Lil" && x.LastName == "Scatchar") ||
                                          (x.Email.Contains("ncowle0@") && x.Email.EndsWith(".com"))).ToArray();
        response.Should().BeEquivalentTo(expected);
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
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Where(x => x.FirstName != "Billy").Take(100));

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

    [Fact]
    public async Task WhenConditionGroups_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>()
                    .Where(x => (x.FirstName == "Billy" && x.LastName == "Montgomery")
                                || (x.Id == 2 || x.Id == 3)));

        //assert
        response.Should().BeEquivalentTo(Persons.Where(x =>
            (x.FirstName == "Billy" && x.LastName == "Montgomery") || (x.Id == 2 || x.Id == 3)));
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