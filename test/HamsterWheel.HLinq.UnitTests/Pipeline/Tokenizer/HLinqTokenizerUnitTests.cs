using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

public partial class HLinqTokenizerUnitTests
{
    private readonly HLinqTokenizer _sut = new(new TestServicesCollection().TokenPossibilities);

    [Fact]
    public void Tokenize_WhenHaveFilterAndPaging_CanParse()
    {
        const string query = "where[x.Id==1].skip[10].take[20]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            ExpectedToken.LeftSquareBracket,
            Entity(),
            ExpectedToken.Dot,
            Prop("Id"),
            Equality,
            NameOrValue("1"),
            ExpectedToken.RightSquareBracket,
            ExpectedToken.Dot,
            Skip,
            ExpectedToken.LeftSquareBracket,
            NameOrValue("10"),
            ExpectedToken.RightSquareBracket,
            ExpectedToken.Dot,
            Take,
            ExpectedToken.LeftSquareBracket,
            NameOrValue("20"),
            ExpectedToken.RightSquareBracket
        ]);
    }
}