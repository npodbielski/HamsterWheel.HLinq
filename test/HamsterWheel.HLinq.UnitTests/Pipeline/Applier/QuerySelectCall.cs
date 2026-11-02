using FluentAssertions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// A call written as a member of the select answers the row the way the same call written by hand
/// in Linq answers it: a rename names the answer, an unnamed call names itself by the method,
/// written arguments and the culture of the request reach the method, and a call nothing can bind
/// is refused by name.
/// </summary>
partial class HLinqQueryApplierUnitTests
{
    [Fact]
    public void Apply_WhenRenamedInstanceCall_ThenAnswersUnderTheWrittenName()
    {
        //act
        var actual = ApplyNested("select[U=x.Name.ToUpper()]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new { U = p.Name.ToUpper() })));
    }

    [Fact]
    public void Apply_WhenUnnamedInstanceCall_ThenAnswersUnderTheMethodName()
    {
        //act
        var actual = ApplyNested("select[x.Name.ToUpper()]");

        //assert
        var row = ((object[])actual)[0];
        row.GetType().GetProperty("ToUpper")!.GetValue(row).Should().Be("ANN");
    }

    [Fact]
    public void Apply_WhenUnnamedCallAmongMembers_ThenEveryMemberIsAnswered()
    {
        //act
        var actual = ApplyNested("select[x.Name.ToUpper(),x.Name]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new { ToUpper = p.Name.ToUpper(), p.Name })));
    }

    [Fact]
    public void Apply_WhenTwoUnnamedCallsOfTheSameMethod_ThenOneColumnAnswersTheLastOfThem()
    {
        var entities = new[] { new DummyEntity("Ann") { Text = "bob" } };

        //act - both calls name themselves `ToUpper` and one column carries the last of them
        var actual = ApplyDummy("select[x.Name.ToUpper(),x.Text.ToUpper()]", entities);

        //assert
        Json(actual).Should().Be(Json(new[] { new { ToUpper = "bob".ToUpper() } }));
    }

    [Fact]
    public void Apply_WhenCallWithWrittenArguments_ThenTheArgumentsReachTheMethod()
    {
        //act
        var actual = ApplyNested("select[R=x.Name.Substring(0,2)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new { R = p.Name.Substring(0, 2) })));
    }

    [Fact]
    public void Apply_WhenCallAnswersATruth_ThenTheTruthIsTheAnswer()
    {
        //act
        var actual = ApplyNested("select[R=x.Name.Contains(An)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new { R = p.Name.Contains("An") })));
    }

    [Fact]
    public void Apply_WhenCallOverMemberUnderGroupBy_ThenRowsOfEachGroupAnswerTheirCall()
    {
        //act
        var actual = ApplyNested("groupBy[x.Name].select[x.Key,U=x.Name.ToUpper()]");

        //assert
        foreach (var group in (object[])actual)
        {
            var rows = (System.Collections.IEnumerable)ValueOf(group, "Rows");
            ValueOf(rows.Cast<object>().Single(), "U")
                .Should().Be(ValueOf(group, "Key").ToString()!.ToUpper());
        }
    }

    [Fact]
    public void Apply_WhenInstanceCallCannotBind_ThenRefusedNamingTheSignatures()
    {
        //act
        var action = () => ApplyNested("select[U=x.Name.ToUpper(7)]");

        //assert
        action.Should().Throw<Method.InvalidMethodException>()
            .Which.Message.Should().Contain("ToUpper()").And.Contain("Name");
    }

    [Fact]
    public void Apply_WhenStaticCallNamesNoPack_ThenRefusedNamingTheEmptySet()
    {
        //act
        var action = () => ApplyNested("select[R=reverse(x.Name)]");

        //assert
        action.Should().Throw<Method.InvalidStaticMethodException>()
            .Which.Message.Should().Contain("reverse");
    }

    [Fact]
    public void Apply_WhenNavigationCountCallWithoutGroup_ThenStillCountsTheCollection()
    {
        //arrange
        var people = People();

        //act
        var actual = ApplyCalls("select[C=x.Addresses.Count()]", people);

        //assert
        Json(actual).Should().Be(Json(people.Select(p => new { C = p.Addresses.Count })));
    }

    [Fact]
    public void Apply_WhenWrittenArgumentIsReadInRequestCulture_ThenTheCultureReachesTheCall()
    {
        //arrange
        var entities = new[]
        {
            new DummyEntity("Ann") { Decimal = 2m },
            new DummyEntity("Bob") { Decimal = 1.2m }
        };

        //act
        var polish = ApplyDummy("""culture=pl-PL&select[C=x.Decimal.CompareTo("1,2")]""", entities);
        var invariant = ApplyDummy("""select[C=x.Decimal.CompareTo("1,2")]""", entities);

        //assert
        Json(polish).Should().Be(Json(entities.Select(e => new { C = decimal.Compare(e.Decimal, 1.2m) })));
        Json(invariant).Should().Be(Json(entities.Select(e => new { C = decimal.Compare(e.Decimal, 12m) })));
    }

    private object ApplyCalls(string queryString, IEnumerable<NestedPerson>? people = null)
    {
        var hlinqQuery = new HLinqQuery<NestedPerson>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        return _sut.Apply((people ?? People()).AsQueryable(), hlinqQuery);
    }

    private static object ApplyDummy(string request, IEnumerable<DummyEntity> entities)
    {
        var services = new ServiceCollection();
        HLinqCore.ConfigureServices(services, new HLinqOptions());
        using var provider = services.BuildServiceProvider();
        var dependencies = provider.GetRequiredService<HLinqBinderDependenciesBag>();
        var hlinqQuery = HLinqQuery<DummyEntity>.Parse(dependencies, request);
        var applier = new HLinqQueryApplier(
            provider.GetRequiredService<IElementApplierFactory>(),
            provider.GetRequiredService<IMethodsCache>(),
            provider.GetRequiredService<IExpressionBuilder>());
        return applier.Apply(entities.AsQueryable(), hlinqQuery);
    }
}
