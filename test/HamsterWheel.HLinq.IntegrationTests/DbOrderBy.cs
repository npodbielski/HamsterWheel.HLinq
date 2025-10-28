using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenEmptyOrderBy_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?orderBy[]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenOrderByString_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().OrderBy(x => x.FirstName));

        //assert
        var expected = Persons.OrderBy(x => x.FirstName).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByStringDescending_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().OrderByDescending(x => x.FirstName));

        //assert
        var expected = Persons.OrderByDescending(x => x.FirstName).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByStringThenByString_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().OrderBy(x => x.FirstName).ThenBy(x => x.LastName));

        //assert
        var expected = Persons.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByStringThenByStringDescending_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db",
            q => q.For<Person>().OrderBy(x => x.FirstName).ThenByDescending(x => x.LastName));

        //assert
        var expected = Persons.OrderBy(x => x.FirstName).ThenByDescending(x => x.LastName).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }
}