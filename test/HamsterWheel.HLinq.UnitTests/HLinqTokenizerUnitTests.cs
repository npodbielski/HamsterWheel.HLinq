using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokenizer;
using static HamsterWheel.HLinq.UnitTests.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests;

public partial class HLinqTokenizerUnitTests
{
    private readonly IHLinqParsersCollection _parsersCollection = new HLinqServicesCollection(new HLinqCore());
    private readonly HLinqTokenizer _sut;

    public HLinqTokenizerUnitTests()
    {
        _sut = new HLinqTokenizer(_parsersCollection);
    }

    [Fact]
    public void Tokenize_WhenSelectWithOneProp_CanParse()
    {
        const string query = "select[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenOnePropRename_CanParse()
    {
        const string query = "select[Mode=x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("Mode"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithConstValueProperty_CanParse()
    {
        const string query = "select[Directory=Core]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithSourcePropAndWithConstValueProperty_CanParse()
    {
        const string query = "select[x.Id,Directory=Core]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Comma,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithConstValuePropertyAndWithSourceProp_CanParse()
    {
        const string query = "select[Directory=Core,x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            Comma,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithTwoProps_CanParse()
    {
        const string query = "select[x.Name, x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Comma,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithSourcePropertyAndConstValueProperty_CanParse()
    {
        const string query = "select[x.Name,Directory=Core]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Comma,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWitNestedProp_CanParse()
    {
        const string query = "select[x.Nested.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Nested"),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("==")]
    [InlineData(">=")]
    [InlineData(">")]
    [InlineData("<=")]
    [InlineData("<")]
    public void Tokenize_WhenWhereSingleComparison_CanParse(string comparison)
    {
        var query = $"where[x.Int{comparison}0]";
        var tokens = _sut.Tokenize(query);
        var comparisonToken = comparison switch
        {
            "==" => Equality,
            ">=" => GreaterOrEqualThan,
            ">" => GreaterThan,
            "<=" => LessOrEqualThan,
            "<" => LessThan,
        };
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Int"),
            comparisonToken,
            NameOrValue("0"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHavePropertyMethod_CanParse()
    {
        const string query = "where[x.Name.Contains(test)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodCall("Contains"),
            LeftCircleBracket,
            NameOrValue("test"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveStaticMethod_CanParse()
    {
        const string query = "where[ilike(x.Name,test)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            MethodCall("ilike"),
            LeftCircleBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Comma,
            NameOrValue("test"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveMethodWith2Params_CanParse()
    {
        //TODO: allow "" for string values or GUIDs
        const string query = "where[x.Name.Contains(test,StringComparison.InvariantCultureIgnoreCase)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodCall("Contains"),
            LeftCircleBracket,
            NameOrValue("test"),
            Comma,
            NameOrValue("StringComparison.InvariantCultureIgnoreCase"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenEqualityOfSingleProperty_CanParse()
    {
        const string query = "where[x.Id==846e7ce6-ef91-43b9-877f-98ad793beca1]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("846e7ce6-ef91-43b9-877f-98ad793beca1"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenEqualityOfSinglePropertyInGroup_CanParse()
    {
        const string query = "where[(x.Id==846e7ce6-ef91-43b9-877f-98ad793beca1)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            LeftCircleBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("846e7ce6-ef91-43b9-877f-98ad793beca1"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenEqualityOfTwoAndProperties_CanParse()
    {
        const string query = "where[x.Id==846e7ce6-ef91-43b9-877f-98ad793beca1&&x.Name==test]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("846e7ce6-ef91-43b9-877f-98ad793beca1"),
            And,
            Entity(),
            Dot,
            Prop("Name"),
            Equality,
            NameOrValue("test"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenEqualityOfTwoAndPropertiesInGroup_CanParse()
    {
        const string query = "where[(x.Id==846e7ce6-ef91-43b9-877f-98ad793beca1&&x.Name==test)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            LeftCircleBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("846e7ce6-ef91-43b9-877f-98ad793beca1"),
            And,
            Entity(),
            Dot,
            Prop("Name"),
            Equality,
            NameOrValue("test"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenEqualityOfTwoOrPropertiesWithOr_CanParse()
    {
        const string query = "where[x.Id==1||x.Name.StartsWith(d)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("1"),
            Or,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodCall("StartsWith"),
            LeftCircleBracket,
            NameOrValue("d"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenEqualityOfTwoOrPropertiesWithOrInGroup_CanParse()
    {
        const string query = "where[(x.Id==1||x.Name.StartsWith(d))]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            LeftCircleBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("1"),
            Or,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodCall("StartsWith"),
            LeftCircleBracket,
            NameOrValue("d"),
            RightCircleBracket,
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenTwoConditionsMethodAndProp_CanParse()
    {
        const string query = "where[x.Name.Contains(test)&&x.Id==77774169-BB9D-4DF9-A4A7-52019C4A445D]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodCall("Contains"),
            LeftCircleBracket,
            NameOrValue("test"),
            RightCircleBracket,
            And,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("77774169-BB9D-4DF9-A4A7-52019C4A445D"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyPaging_CanParse()
    {
        const string query = "skip[10].take[20]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Skip,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket,
            Dot,
            Take,
            LeftSquareBracket,
            NameOrValue("20"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlySkip_CanParse()
    {
        const string query = "skip[10]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Skip,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyTake_CanParse()
    {
        const string query = "take[10]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveTakeSkip_CanParse()
    {
        const string query = "take[10].skip[1]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket,
            Dot,
            Skip,
            LeftSquareBracket,
            NameOrValue("1"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveFilterAndPaging_CanParse()
    {
        const string query = "where[x.Id==1].skip[10].take[20]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Equality,
            NameOrValue("1"),
            RightSquareBracket,
            Dot,
            Skip,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket,
            Dot,
            Take,
            LeftSquareBracket,
            NameOrValue("20"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOrderthenBy_CanParse()
    {
        const string query = "orderBy[x.Name].thenBy[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrder_CanParse()
    {
        const string query = "orderBy[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrderByDescending_CanParse()
    {
        const string query = "orderByDescending[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrderByDescendingthenBy_CanParse()
    {
        const string query = "orderByDescending[x.Name].thenBy[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrderBythenByDesc_CanParse()
    {
        const string query = "orderBy[x.Name].thenByDescending[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrderByDescthenByDesc_CanParse()
    {
        const string query = "orderByDescending[x.Name].thenByDescending[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }
}

//todo: consider allowing where[] as a safe start to work with filters like 1==1 in SQL