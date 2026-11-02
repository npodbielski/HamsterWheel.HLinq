using System.Collections;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// A nested select answers a shaped collection (or a shaped single row) per row of the answer.
/// Every result is compared with what the same Linq written by hand answers - the oracle of the
/// tree built from the query text.
/// </summary>
partial class HLinqQueryApplierUnitTests
{
    [Fact]
    public void Apply_WhenNestedSelectOverCollection_ThenAnswersShapedListPerRow()
    {
        //act
        var actual = ApplyNested("select[x.Name,addresses=x.Addresses.select(a.Street,a.City)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            p.Name,
            addresses = p.Addresses.Select(a => new { a.Street, a.City }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenNestedSelectWithoutName_ThenAnswersUnderNavigationName()
    {
        //act
        var actual = ApplyNested("select[x.Addresses.select(a.Street)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            Addresses = p.Addresses.Select(a => new { a.Street }).ToList()
        })));
        var first = ((object[])actual)[0];
        first.GetType().GetProperty("Addresses")!.GetValue(first).Should().NotBeNull();
    }

    [Fact]
    public void Apply_WhenNestedSelectRenamed_ThenAnswersUnderTheWrittenName()
    {
        //act
        var actual = ApplyNested("select[x.Name,delivery=x.Addresses.select(city=a.City)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            p.Name,
            delivery = p.Addresses.Select(a => new { city = a.City }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenNestedSelectOverReference_ThenAnswersOneShapedRow()
    {
        //act
        var actual = ApplyNested("select[x.Address.select(a.Street,a.City)]");

        //assert
        var rows = (object[])actual;
        var address = rows[0].GetType().GetProperty("Address")!.GetValue(rows[0]);
        Json(address).Should().Be(Json(new { Street = "Main 1", City = "Warsaw" }));
        rows[1].GetType().GetProperty("Address")!.GetValue(rows[1]).Should().BeNull();
    }

    [Fact]
    public void Apply_WhenOuterMemberCapturedInsideNestedSelect_ThenEachInnerRowCarriesIt()
    {
        //act
        var actual = ApplyNested("select[x.Name,address=x.Addresses.select(contact=x.Name,a.City)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            p.Name,
            address = p.Addresses.Select(a => new { contact = p.Name, a.City }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenNestedSelectUnderNestedSelect_ThenShapesNestOneInsideAnother()
    {
        //act
        var actual = ApplyNested("select[x.Addresses.select(a.Street,country=a.Country.select(c.Name))]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            Addresses = p.Addresses.Select(a => new
            {
                a.Street,
                country = a.Country is null ? null : new { a.Country!.Name }
            }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenBareInnerName_ThenMeansTheElementMember()
    {
        //act
        var actual = ApplyNested("select[x.Addresses.select(Street)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            Addresses = p.Addresses.Select(a => new { a.Street }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenNestedSelectFollowedByMember_ThenBothAreAnswered()
    {
        //act
        var actual = ApplyNested("select[x.Addresses.select(a.Street),x.Name]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            Addresses = p.Addresses.Select(a => new { a.Street }).ToList(),
            p.Name
        })));
    }

    [Fact]
    public void Apply_WhenNestedSelectAfterGroupBy_ThenRowsOfEachGroupTakeTheShape()
    {
        //act - the grouped rows are still the entity's rows, so the call resolves over them
        //the same way it does anywhere else
        var actual = ApplyNested("groupBy[x.Name].select[x.Key,addresses=x.Addresses.select(a.City)]");

        //assert
        var groups = ((object[])actual).ToDictionary(
            g => g.GetType().GetProperty("Key")!.GetValue(g)!.ToString()!,
            g => ((IEnumerable)g.GetType().GetProperty("Rows")!.GetValue(g)!)
                .Cast<object>().ToList());
        Json(groups["Ann"][0]).Should().Be(Json(new
        {
            Key = "Ann",
            addresses = People()[0].Addresses.Select(a => new { a.City }).ToList()
        }));
    }

    [Fact]
    public void Apply_WhenNestedSelectHasNoMembers_ThenRefused()
    {
        //a call with nothing between the brackets shapes no member at all
        var action = () => ApplyNested("select[x.Addresses.select()]");

        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Apply_WhenInnerNameIsNoMember_ThenRefusedNamingIt()
    {
        //act
        var action = () => ApplyNested("select[x.Addresses.select(a.Nope)]");

        //assert
        action.Should().Throw<Exception>().Which.Message.Should().Contain("Nope");
    }

    [Fact]
    public void Apply_WhenSelectOfNestedSelectWithWhereAndOrderBy_ThenComposes()
    {
        //act - the order and the filter stand before the select, as they do for every projection:
        //the select reshapes the rows and there is nothing left to order or filter by after it
        var actual = ApplyNested("where[x.Name==Ann].orderBy[x.Name].select[x.Addresses.select(a.City)]");

        //assert
        var rows = (object[])actual;
        rows.Should().HaveCount(1);
        Json(rows[0]).Should().Be(Json(new
        {
            Addresses = People()[0].Addresses.Select(a => new { a.City }).ToList()
        }));
    }

    private static NestedPerson[] People() =>
    [
        new("Ann")
        {
            Address = new NestedAddress
            {
                Street = "Main 1", City = "Warsaw", Country = new NestedCountry { Name = "PL" }
            },
            Addresses =
            [
                new NestedAddress
                {
                    Street = "Main 1", City = "Warsaw", Country = new NestedCountry { Name = "PL" }
                },
                new NestedAddress
                {
                    Street = "Oak 2", City = "Krakow", Country = new NestedCountry { Name = "PL" }
                }
            ]
        },
        new("Bob") { Address = null, Addresses = [] }
    ];

    private object ApplyNested(string queryString)
    {
        var hlinqQuery = new HLinqQuery<NestedPerson>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        return _sut.Apply(People().AsQueryable(), hlinqQuery);
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
