using FluentAssertions;
using HamsterWheel.HLinq.Tree.Filtering;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// A call inside a nested select reads whichever scope its name hangs on - the inner row's member,
/// or the outer row's - the same rule an inner property rename and a nested aggregate already
/// follow, and the reason root and nested select are one code path.
/// </summary>
partial class HLinqQueryApplierUnitTests
{
    [Fact]
    public void Apply_WhenInnerCallHangsOnInnerMember_ThenEachInnerRowAnswersItsOwn()
    {
        //act
        var actual = ApplyNested("select[x.Addresses.select(City=a.City.ToUpper())]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            Addresses = p.Addresses.Select(a => new { City = a.City.ToUpper() }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenInnerCallHangsOnOuterMember_ThenEveryInnerRowCarriesTheOuterRow()
    {
        //act
        var actual = ApplyNested("select[x.Name,address=x.Addresses.select(contact=x.Name.ToUpper(),a.City)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            p.Name,
            address = p.Addresses.Select(a => new { contact = p.Name.ToUpper(), a.City }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenInnerUnnamedCall_ThenTheInnerAnswerNamesItselfByTheMethod()
    {
        //act
        var actual = ApplyNested("select[x.Addresses.select(a.City.ToUpper())]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            Addresses = p.Addresses.Select(a => new { ToUpper = a.City.ToUpper() }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenInnerCallWithWrittenArguments_ThenTheArgumentsReachTheInnerMethod()
    {
        //act
        var actual = ApplyNested("select[x.Addresses.select(S=a.City.Substring(0,1))]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            Addresses = p.Addresses.Select(a => new { S = a.City.Substring(0, 1) }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenInnerAggregateCountsTheOuterCollection_ThenEachInnerRowAnswersTheOuterRow()
    {
        //arrange
        var people = People();

        //act
        var actual = ApplyCalls("select[x.Addresses.select(C=x.Addresses.Count())]", people);

        //assert
        Json(actual).Should().Be(Json(people.Select(p => new
        {
            Addresses = p.Addresses.Select(_ => new { C = p.Addresses.Count }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenInnerStaticCallNamesNoPack_ThenRefusedNamingTheEmptySet()
    {
        //act
        var action = () => ApplyNested("select[x.Addresses.select(City=upper(x.Name))]");

        //assert
        action.Should().Throw<Method.InvalidStaticMethodException>()
            .Which.Message.Should().Contain("upper");
    }

    [Fact]
    public void Apply_WhenInnerCallCannotBind_ThenRefusedNamingTheSignatures()
    {
        //act
        var action = () => ApplyNested("select[x.Addresses.select(City=a.City.ToUpper(7))]");

        //assert
        action.Should().Throw<Method.InvalidMethodException>()
            .Which.Message.Should().Contain("ToUpper()");
    }
}
