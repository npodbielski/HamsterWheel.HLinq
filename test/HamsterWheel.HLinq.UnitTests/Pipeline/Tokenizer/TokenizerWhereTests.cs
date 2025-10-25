using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

partial class HLinqTokenizerUnitTests
{
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
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null)
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
            MethodName("Contains"),
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
            MethodName("ilike"),
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
        const string query = "where[x.Name.Contains(test,StringComparison.InvariantCultureIgnoreCase)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodName("Contains"),
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
            MethodName("StartsWith"),
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
            MethodName("StartsWith"),
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
            MethodName("Contains"),
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
}