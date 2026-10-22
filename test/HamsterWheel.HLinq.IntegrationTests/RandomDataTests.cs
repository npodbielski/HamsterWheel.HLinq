using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
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

    [Fact]
    public async Task WhenQuotedIntReachesItsType_ThenCanFilter() =>
        await FilterAndAssert("""where[x.Int>"5"]""", x => x.Int > 5);

    [Fact]
    public async Task WhenQuotedDoubleReachesItsType_ThenCanFilter() =>
        await FilterAndAssert("""where[x.Double>"1.5"]""", x => x.Double > 1.5);

    [Fact]
    public async Task WhenQuotedDecimalReachesItsType_ThenCanFilter() =>
        await FilterAndAssert("""where[x.Decimal>"1.5"]""", x => x.Decimal > 1.5m);

    private async Task FilterAndAssert(string query, Func<RandomData, bool> expected)
    {
        //act
        var response = await fixture.Client.GetAsync($"/demo/random?{query}");

        //assert - the quotes say the text is one value, they do not make it a string
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<RandomData[]>(
            ResponseHLinqClientQueryBuilder.DefaultJsonSerializerOptions);
        data.Should().BeEquivalentTo(RandomData.Get().Where(expected));
    }

    [Fact]
    public async Task WhenQuotedCommaReachesADecimal_ThenItGroupsThousands() =>
        await FilterAndAssert("""where[x.Decimal>"1,2"]""", x => x.Decimal > 12m);

    [Fact]
    public async Task WhenExponentReachesADouble_ThenItIsAPowerOfTen() =>
        await FilterAndAssert("where[x.Double>1e3]", x => x.Double > 1_000);

    [Fact]
    public async Task WhenNumberIsWrittenInParentheses_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/random?where[x.Int>(5)]");

        //assert - `(5)` is not how the query language writes minus five
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenQuotedValueIsNotANumber_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/random?where[x.Int>\"abc\"]");

        //assert - quoting asks for the text to be read as an Int32, it does not exempt it from being one
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}