using FluentAssertions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

public partial class HLinqQueryApplierUnitTests
{
    private readonly IHLinqTokenizer _tokenizer = ServicesCollection.Tokenizer;
    private readonly HLinqParser _parser = new(ServicesCollection.Parsers);
    private static readonly TestServicesCollection ServicesCollection = new();

    private readonly HLinqQueryApplier _sut = ServicesCollection.NewApplier();

    [Fact]
    public void Apply_WhenNullDataAndNullCount_ThenThrows()
    {
        //arrange
        var action = () => _sut.Apply(null!, typeof(int), new HLinqQuery<int>());

        //act
        var exception = action.Should().Throw<InvalidApplierResultException>();

        //assert
        exception.WithMessage("*'Data' and 'Count' null");
    }
}