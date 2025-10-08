using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tree.Select;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.Dummies;

namespace HamsterWheel.HLinq.UnitTests;

public partial class ExpressionBuilderUnitTests
{
    [Fact]
    public void GetSelect_WhenNoProp_ThenThrows()
    {
        const string query = "select[]";
        var tokens = _tokenizer.Tokenize(query);

        var action = () => _parser.Parse<DummyEntity>(tokens, query);

        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void GetSelect_WhenSingleProp_ThenCanSelect()
    {
        const string query = "select[x.Name]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse<DummyEntity>(tokens, query);
        var actual =
            _sut.GetSelect(typeof(DummyEntity), tree.GetAll<SelectRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should().Match("Param_0 => Param_0.Name");
        var select = actual.Compile();
        var entity = new DummyEntity("test");
        select.Method.Invoke(null, [select.Target, entity]).Should().Be(entity.Name);
    }

    [Fact]
    public void GetSelect_WhenPropRename_ThenCanSelect()
    {
        const string query = "select[Mode=x.Name]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse<DummyEntity>(tokens, query);
        var actual =
            _sut.GetSelect(typeof(DummyEntity), tree.GetAll<SelectRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should().Match("Param_0 => new DynamicAnonymousType*`1() {Mode = Param_0.Name}");
        var select = actual.Compile();
        var entity = new DummyEntity("test");
        select.Method.Invoke(null, [select.Target, entity]).Should().BeEquivalentTo(new { Mode = entity.Name });
    }

    [Fact]
    public void GetSelect_WhenNestedProp_ThenCanSelect()
    {
        const string query = "select[x.Nested.Name]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse<DummyEntity>(tokens, query);
        var actual = _sut.GetSelect(typeof(DummyEntity), tree.GetAll<SelectRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Match("Param_0 => Param_0.Nested.Name");
        var select = actual.Compile();
        var entity = new DummyEntity("test")
        {
            Nested = new NestedDummyEntity
            {
                Id = Guid.NewGuid(),
                Name = "nested"
            }
        };
        select.Method.Invoke(null, [select.Target, entity]).Should().Be(entity.Nested.Name);
    }

    [Fact]
    public void GetSelect_WhenTwoProps_ThenCanSelect()
    {
        const string query = "select[x.Name,x.Flag]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse<DummyEntity>(tokens, query);
        var actual =
            _sut.GetSelect(typeof(DummyEntity), tree.GetAll<SelectRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Match("Param_0 => new DynamicAnonymousType*`2() {Name = Param_0.Name, Flag = Param_0.Flag}");
        var select = actual.Compile();
        var entity = new DummyEntity("test");
        select.Method.Invoke(null, [select.Target, entity]).Should().BeEquivalentTo(new { entity.Name, entity.Flag });
    }

    [Fact]
    public void GetSelect_WhenThreeProps_ThenCanSelect()
    {
        const string query = "select[x.Name,x.Flag,x.DateTimeOffset]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse<DummyEntity>(tokens, query);
        var actual =
            _sut.GetSelect(typeof(DummyEntity), tree.GetAll<SelectRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Match(
                "Param_0 => new DynamicAnonymousType*`3() {Name = Param_0.Name, Flag = Param_0.Flag, DateTimeOffset = Param_0.DateTimeOffset}");
        var select = actual.Compile();
        var entity = new DummyEntity("test");
        select.Method.Invoke(null, [select.Target, entity]).Should()
            .BeEquivalentTo(new { entity.Name, entity.Flag, entity.DateTimeOffset });
    }

    [Fact]
    public void GetSelect_When27Props_ThenCanSelect()
    {
        const string query = "select[x.Id,x.Name,x.Flag,x.NullableFlag,x.Byte,x.Short,x.Int,x.Long,x.Enum,x.Decimal," +
                             "x.Double,x.Float,x.NullableByte,x.NullableShort,x.NullableInt,x.NullableLong," +
                             "x.NullableEnum,x.NullableDecimal,x.NullableDouble,x.NullableFloat,x.NullableString,x.Time," +
                             "x.DateTime,x.DateTimeOffset,x.NullableTime,x.NullableDateTime,x.NullableDateTimeOffset]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.Parse<DummyEntity>(tokens, query);
        var actual =
            _sut.GetSelect(typeof(DummyEntity), tree.GetAll<SelectRoot>().First(), query).Expression;
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Match(
                "Param_0 => new DynamicAnonymousType*`27() {Id = Param_0.Id, Name = Param_0.Name, Flag = Param_0.Flag, " +
                "NullableFlag = Param_0.NullableFlag, Byte = Param_0.Byte, Short = Param_0.Short, " +
                "Int = Param_0.Int, Long = Param_0.Long, Enum = Param_0.Enum, Decimal = Param_0.Decimal, " +
                "Double = Param_0.Double, Float = Param_0.Float, NullableByte = Param_0.NullableByte, " +
                "NullableShort = Param_0.NullableShort, NullableInt = Param_0.NullableInt, " +
                "NullableLong = Param_0.NullableLong, NullableEnum = Param_0.NullableEnum, " +
                "NullableDecimal = Param_0.NullableDecimal, NullableDouble = Param_0.NullableDouble, " +
                "NullableFloat = Param_0.NullableFloat, NullableString = Param_0.NullableString, " +
                "Time = Param_0.Time, DateTime = Param_0.DateTime, DateTimeOffset = Param_0.DateTimeOffset, " +
                "NullableTime = Param_0.NullableTime, NullableDateTime = Param_0.NullableDateTime, " +
                "NullableDateTimeOffset = Param_0.NullableDateTimeOffset}");
        var select = actual.Compile();
        var entity = new DummyEntity("test");
        select.Method.Invoke(null, [select.Target, entity]).Should()
            .BeEquivalentTo(new
            {
                entity.Id,
                entity.Name,
                entity.Flag,
                entity.NullableFlag,
                entity.Byte,
                entity.Short,
                entity.Int,
                entity.Long,
                entity.Enum,
                entity.Decimal,
                entity.NullableShort,
                entity.NullableInt,
                entity.NullableLong,
                entity.NullableEnum,
                entity.NullableDecimal,
                entity.NullableDouble,
                entity.NullableFloat,
                entity.NullableString,
                entity.Time,
                entity.DateTime,
                entity.DateTimeOffset,
                entity.NullableTime,
                entity.NullableDateTime,
                entity.NullableDateTimeOffset
            });
    }
}