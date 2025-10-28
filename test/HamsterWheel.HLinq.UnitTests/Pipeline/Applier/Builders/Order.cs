using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Ordering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier.Builders;

public partial class ExpressionBuilderUnitTests
{
    [Fact]
    public void GetProperty_WhenStringProp_ThenCanSelect()
    {
        const string query = "orderBy[x.Name]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        };
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse(hlinqQuery, tokens);
        var actual =
            _sut.GetProperty(typeof(DummyEntity), 
                tree.GetAll<OrderByRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should().Match("Param_0 => Param_0.Name");
        var propertySelect = actual.Compile();
        var entity = new DummyEntity("test");
        propertySelect.Method.Invoke(null, [propertySelect.Target, entity])
            .Should().Be(entity.Name);
    }
    
    [Fact]
    public void GetProperty_WhenBoolProp_ThenCanSelect()
    {
        const string query = "orderBy[x.Flag]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        };
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse(hlinqQuery, tokens);
        var actual =
            _sut.GetProperty(typeof(DummyEntity), 
                tree.GetAll<OrderByRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should().Match("Param_0 => Param_0.Flag");
        var propertySelect = actual.Compile();
        var entity = new DummyEntity("test");
        propertySelect.Method.Invoke(null, [propertySelect.Target, entity])
            .Should().Be(entity.Flag);
    }
    
    [Fact]
    public void GetProperty_WhenEnumProp_ThenCanSelect()
    {
        const string query = "orderBy[x.Enum]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        };
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse(hlinqQuery, tokens);
        var actual =
            _sut.GetProperty(typeof(DummyEntity), 
                tree.GetAll<OrderByRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should().Match("Param_0 => Param_0.Enum");
        var propertySelect = actual.Compile();
        var entity = new DummyEntity("test");
        propertySelect.Method.Invoke(null, [propertySelect.Target, entity])
            .Should().Be(entity.Enum);
    }
    
    [Fact]
    public void GetProperty_WhenNullableDateTimeOffsetProp_ThenCanSelect()
    {
        const string query = "orderBy[x.NullableDateTimeOffset]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        };
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse(hlinqQuery, tokens);
        var actual =
            _sut.GetProperty(typeof(DummyEntity), 
                tree.GetAll<OrderByRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should().Match("Param_0 => Param_0.NullableDateTimeOffset");
        var propertySelect = actual.Compile();
        var entity = new DummyEntity("test");
        propertySelect.Method.Invoke(null, [propertySelect.Target, entity])
            .Should().Be(entity.NullableDateTimeOffset);
    }
}