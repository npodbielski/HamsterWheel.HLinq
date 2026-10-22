using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Reflection;

public class MemberResolutionUnitTests
{
    private static readonly TestServicesCollection Services = new();
    private readonly IHLinqParser _parser = new HLinqParser(Services.Parsers);

    private readonly ExpressionBuilder _sut = new(Services.PropertiesCache, Services.MethodsCache,
        Services.ConverterFactory, new ParametersConverter(), []);

    private static (string value, MemberExpression? expression)[] Text(params string[] values) =>
        [.. values.Select(v => (v, (MemberExpression?)null))];

    private LambdaExpression Filter(string query)
    {
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = _parser.TestParseEntryPoint<RankedEntity>(query, tokens);

        return _sut.GetFilter(typeof(RankedEntity), tree.GetAll<WhereRoot>().First(), query);
    }

    [Fact]
    public void Resolve_WhenTwoPropertiesDifferOnlyInCase_ThenTheOneSpelledAsTheQueryWroteItWins()
    {
        var asWritten = Services.PropertiesCache.Resolve(typeof(RankedEntity), "Name");
        var other = Services.PropertiesCache.Resolve(typeof(RankedEntity), "name");

        asWritten.Member!.Name.Should().Be("Name");
        asWritten.Reason.Should().Be(ResolutionReason.SpelledExactlyAsWritten);
        other.Member!.Name.Should().Be("name");
        other.Reason.Should().Be(ResolutionReason.SpelledExactlyAsWritten);
        asWritten.Competitors.Select(p => p.Name).Should().BeEquivalentTo(["Name", "name"]);
    }

    [Fact]
    public void Resolve_WhenNeitherSpellingIsTheOneTheQueryWrote_ThenTheOrdinalFirstOneWins()
    {
        var resolution = Services.PropertiesCache.Resolve(typeof(RankedEntity), "nAmE");

        resolution.Member!.Name.Should().Be("Name");
        resolution.Reason.Should().Be(ResolutionReason.OrdinalTieBreak);
    }

    [Fact]
    public void Single_WhenANameCompetes_ThenItStaysTheMemberOfTheResolution()
    {
        Services.PropertiesCache.Single(typeof(RankedEntity), "Name")
            .Should().BeSameAs(Services.PropertiesCache.Resolve(typeof(RankedEntity), "Name").Member);
    }

    [Fact]
    public void ResolveMethod_WhenTheTypeShadowsAnInheritedMethod_ThenTheOneItDeclaresWins()
    {
        var resolution = _sut.ResolveMethod(typeof(RankedDerived), "Flag", Text("abc"));

        resolution.Member!.DeclaringType.Should().Be(typeof(RankedDerived));
        resolution.Reason.Should().Be(ResolutionReason.DeclaredOnType);
    }

    [Fact]
    public void ResolveMethod_WhenTheTypeShadowsAMemberOfObject_ThenTheOneItDeclaresWins()
    {
        var resolution = _sut.ResolveMethod(typeof(RankedService), "ToString", []);

        resolution.Member!.DeclaringType.Should().Be(typeof(RankedService));
        resolution.Reason.Should().Be(ResolutionReason.DeclaredOnType);
    }

    [Fact]
    public void ResolveMethod_WhenOnlyObjectDeclaresTheMethod_ThenItStillAnswers()
    {
        var resolution = _sut.ResolveMethod(typeof(RankedService), "GetHashCode", []);

        resolution.Member!.DeclaringType.Should().Be(typeof(object));
        resolution.Reason.Should().Be(ResolutionReason.OnlyCandidate);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("abc")]
    public void ResolveMethod_WhenASingleOverloadTakesTheTextOfTheQuery_ThenItWins(string value)
    {
        var resolution = _sut.ResolveMethod(typeof(RankedService), "Sub", Text(value));

        resolution.Member!.GetParameters().Single().ParameterType.Should().Be(typeof(string));
        resolution.Reason.Should().Be(ResolutionReason.OnlyBindableCandidate);
        resolution.Competitors.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("One")]
    public void ResolveMethod_WhenSeveralOverloadsTakeTheText_ThenTheOneNeedingNoConversionWins(string value)
    {
        var resolution = _sut.ResolveMethod(typeof(RankedService), "Sub2", Text(value));

        resolution.Member!.GetParameters().Single().ParameterType.Should().Be(typeof(string));
        resolution.Reason.Should().Be(ResolutionReason.CheapestConversion);
    }

    [Fact]
    public void ResolveMethod_WhenTheOnlyOverloadTakesAChar_ThenOneCharacterOfTextFillsIt()
    {
        var resolution = _sut.ResolveMethod(typeof(RankedService), "Sub3", Text("a"));

        resolution.Member!.GetParameters().Single().ParameterType.Should().Be(typeof(char));
        resolution.Reason.Should().Be(ResolutionReason.OnlyBindableCandidate);
    }

    [Fact]
    public void ResolveMethod_WhenAPropertyOfTheQueryReachesAnExactAndABaseParameter_ThenTheExactOneWins()
    {
        var source = Expression.Parameter(typeof(RankedEntity));
        var name = Expression.Property(source, typeof(RankedEntity).GetProperty("Name")!);

        var resolution = _sut.ResolveMethod(typeof(RankedService), "Sub4", [(nameof(RankedEntity.Name), name)]);

        resolution.Member!.GetParameters().Single().ParameterType.Should().Be(typeof(string));
        resolution.Reason.Should().Be(ResolutionReason.CheapestConversion);
    }

    [Fact]
    public void GetStatic_WhenTheSelectorLeavesTwoOverloadsStanding_ThenTheOrdinalFirstOneAnswersInsteadOfThrowing()
    {
        var methods = new MethodsCache();

        //the string overload is the one RankedStatics declares first, so only the ordinal tie-break can
        //answer this question with the same method every time
        var method = methods.GetStatic(typeof(RankedStatics), "Flag", _ => true);

        method!.GetParameters().Single().ParameterType.Should().Be(typeof(char));
    }

    [Fact]
    public void GetFilter_WhenTwoOverloadsOfAPropertyMethodBind_ThenTheCallGoesToTheRankedFirstOne()
    {
        var body = Filter("where[x.Service.Sub2(One)]").Body.Should().BeAssignableTo<MethodCallExpression>().Subject;

        body.Method.GetParameters().Single().ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void GetFilter_WhenANumberIsTheArgumentOfAPropertyMethod_ThenItReachesTheTextOverload()
    {
        var body = Filter("where[x.Service.Sub(5)]").Body.Should().BeAssignableTo<MethodCallExpression>().Subject;

        body.Method.GetParameters().Single().ParameterType.Should().Be(typeof(string));
        var argument = body.Arguments.Single().Should().BeAssignableTo<ConstantExpression>().Subject;
        argument.Value.Should().Be("5");
        argument.Type.Should().Be(typeof(string));
    }
}

public class RankedEntity
{
    public string Name { get; set; } = null!;
    public string name { get; set; } = null!;
    public RankedService Service { get; set; } = new();
}

public class RankedService
{
    public bool Sub(int value) => true;
    public bool Sub(long value) => true;
    public bool Sub(string value) => true;
    public bool Sub2(string value) => true;
    public bool Sub2(char value) => true;
    public bool Sub2(RankedEnum value) => true;
    public bool Sub3(char value) => true;
    public bool Sub3(RankedEnum value) => true;
    public bool Sub4(string value) => true;
    public bool Sub4(object value) => true;
    public bool Sub4(RankedEnum value) => true;
    public new string ToString() => nameof(RankedService);
}

public class RankedBase
{
    public bool Flag(string value) => true;
}

public class RankedDerived : RankedBase
{
    public new bool Flag(string value) => true;
}

public static class RankedStatics
{
    public static bool Flag(string value) => true;
    public static bool Flag(char value) => true;
}

public enum RankedEnum
{
    Zero = 0,
    One = 1
}
