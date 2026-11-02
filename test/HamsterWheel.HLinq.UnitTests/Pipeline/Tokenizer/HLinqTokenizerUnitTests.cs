using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

public partial class HLinqTokenizerUnitTests
{
    private readonly IHLinqTokenizer _sut = new TestServicesCollection().Tokenizer;

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
}