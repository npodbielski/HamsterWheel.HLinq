using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenGroupBy_ThenAnswersAListOfKeyAndRowsEntries()
    {
        //act - a name shared by a few people, so the grouping is small enough to compare whole
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().Where(x => x.FirstName == "Reginald").GroupBy(x => x.LastName));

        //assert
        var expected = Persons.Where(x => x.FirstName == "Reginald")
            .GroupBy(p => p.LastName)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Id).OrderBy(id => id).ToList());
        response.Select(e => e.Key).Should().BeEquivalentTo(expected.Keys);
        response.Should().OnlyContain(entry =>
            entry.Rows.Select(p => p.Id).OrderBy(id => id).SequenceEqual(expected[entry.Key]));
    }

    [Fact]
    public async Task WhenGroupByWithSelect_ThenRowsTakeTheSelectedShape()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().Where(x => x.FirstName == "Reginald").GroupBy(x => x.LastName)
                .Select(x => x.FirstName));

        //assert
        var expected = Persons.Where(x => x.FirstName == "Reginald")
            .GroupBy(p => p.LastName)
            .ToDictionary(g => g.Key, g => g.Select(p => p.FirstName).ToList());
        response.ToDictionary(e => e.Key, e => e.Rows).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenNestedGroupBy_ThenAnswersGroupsUnderGroups()
    {
        //act - the client builder stops at one grouping, the query itself nests two
        var response = await fixture.Client.GetAsync("/demo/db?groupBy[x.LastName].groupBy[x.FirstName]");
        response.EnsureSuccessStatusCode();
        using var answer = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        //assert - every entry is a group whose rows are groups whose rows are people
        var outer = answer.RootElement.EnumerateArray().ToArray();
        outer.Should().NotBeEmpty();
        var sample = outer[0];
        sample.GetProperty("key").GetString().Should().NotBeNullOrEmpty();
        var inner = sample.GetProperty("rows").EnumerateArray().ToArray();
        inner.Should().NotBeEmpty();
        inner[0].GetProperty("key").GetString().Should().NotBeNullOrEmpty();
        inner[0].GetProperty("rows").EnumerateArray().First().GetProperty("lastName")
            .GetString().Should().Be(sample.GetProperty("key").GetString());
    }

    [Fact]
    public async Task WhenGroupByThenTake_ThenKeepsGroupsNotRows()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().GroupBy(x => x.LastName).Take(2));

        //assert
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenGroupByThenCount_ThenCountsGroups()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().GroupBy(x => x.LastName).Count());

        //assert
        response.Should().Be(Persons.Select(p => p.LastName).Distinct().Count());
    }

    [Fact]
    public async Task WhenGroupByThenOrderByKey_ThenOrdersGroupsByKey()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().GroupBy(x => x.LastName).OrderByKey().Take(100));

        //assert
        response.Select(e => e.Key).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task WhenGroupByThenWhereOnKey_ThenKeepsMatchingGroups()
    {
        //arrange
        var lastName = Persons.First().LastName;

        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().GroupBy(x => x.LastName).WhereKey(x => x == lastName));

        //assert
        response.Select(e => e.Key).Should().BeEquivalentTo([lastName]);
        response.Single().Rows.Select(p => p.Id).Should()
            .BeEquivalentTo(Persons.Where(p => p.LastName == lastName).Select(p => p.Id));
    }
}
