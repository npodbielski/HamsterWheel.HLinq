using FluentAssertions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

public partial class HLinqQueryApplierUnitTests
{
    private readonly IHLinqTokenizer _tokenizer = ServicesCollection.Tokenizer;
    private readonly HLinqParser _parser = new(ServicesCollection.Parsers);
    private static readonly TestServicesCollection ServicesCollection = new();

    private readonly HLinqQueryApplier _sut =
        new(ServicesCollection.Provider.GetRequiredService<IElementApplierFactory>(),
            ServicesCollection.Provider.GetRequiredService<IMethodsCache>(),
            ServicesCollection.Provider.GetRequiredService<IExpressionBuilder>());

    [Fact]
    public void Apply_WhenNullDataAndNullCount_ThenThrows()
    {
        //arrange
        var expected = "";
        var action = () => _sut.Apply(null!, typeof(int), new HLinqQuery<int>());

        //act
        var exception = action.Should().Throw<InvalidApplierResultException>();

        //assert
        exception.WithMessage("*'Data' and 'Count' null");
    }
}