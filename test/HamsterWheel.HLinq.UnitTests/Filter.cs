using System.Globalization;
using System.Linq.Expressions;
using AutoFixture.Xunit2;
using FluentAssertions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.Dummies;

namespace HamsterWheel.HLinq.UnitTests;

public partial class ExpressionBuilderUnitTests
{
    [Fact]
    public void GetFilter_WhenStringContains_ThenCanFilter()
    {
        const string query = "where[x.Name.Contains(test)]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => Param_0.Name.Contains(\"test\")");
        var filter = actual.Compile();
        filter.Invoke(new DummyEntity("test")).Should().Be(true);
    }

    [Fact]
    public void GetFilter_WhenStringStartsWith_ThenCanFilter()
    {
        const string query = "where[x.Name.StartsWith(t)]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => Param_0.Name.StartsWith(\"t\")");
        var filter = actual.Compile();
        filter.Invoke(new DummyEntity("test")).Should().Be(true);
    }

    [Fact]
    public void GetFilter_WhenStringEndsWith_ThenCanFilter()
    {
        const string query = "where[x.Name.EndsWith(n)]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => Param_0.Name.EndsWith(\"n\")");
        var filter = actual.Compile();
        filter.Invoke(new DummyEntity("Jan")).Should().Be(true);
    }

    [Fact]
    public void GetFilter_WhenStringContainsIgnoreCase_ThenCanFilter()
    {
        const string query = "where[x.Name.Contains(test, StringComparison.InvariantCultureIgnoreCase)]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => Param_0.Name.Contains(\"test\", InvariantCultureIgnoreCase)");
        var filter = actual.Compile();
        var entity = new DummyEntity("Test");
        filter.Invoke(entity).Should().Be(entity.Name.Contains("test", StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public void GetFilter_WhenContainsViaIlike_ThenCanFilter()
    {
        const string query = "where[ilike(x.Name, test)]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => null.ILike(Param_0.Name, \"test\")");
    }

    [Fact]
    public void GetFilter_WhenStringPropertyEquals_ThenReturnsLambda()
    {
        const string query = "where[x.Name==test]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => (Param_0.Name == \"test\")");
        var filter = actual.Compile();
        filter.Invoke(new DummyEntity("test")).Should().BeTrue();
    }

    [Fact]
    public void GetFilter_WhenStringPropertyNotEquals_ThenReturnsLambda()
    {
        const string query = "where[x.Name!=test]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => (Param_0.Name != \"test\")");
        var filter = actual.Compile();
        filter.Invoke(new DummyEntity("test")).Should().BeFalse();
    }

    [Theory]
    [InlineAutoData("test")]
    public void GetFilter_WhenNullableStringPropertyEqualsWithNotNull_ThenReturnsLambda(string? name)
    {
        var query = $"where[x.NullableString=={name ??= "null"}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Be($"Param_0 => (Param_0.NullableString == {(name is null ? "null" : name.Quote())})");
        var filter = actual.Compile();
        var dummyEntity = new DummyEntity("test");
        filter.Invoke(dummyEntity).Should().Be(dummyEntity.NullableString == name);
    }

    [Fact]
    public void GetFilter_WhenNullableStringPropertyEqualsWithNull_ThenReturnsLambda()
    {
        const string query = "where[x.NullableString==null]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => (Param_0.NullableString == null)");
        var filter = actual.Compile();
        var dummyEntity = new DummyEntity("test");
        filter.Invoke(dummyEntity).Should().Be(dummyEntity.NullableString == null);
    }

    [Theory]
    [AutoData]
    public void GetFilter_WhenGuidPropertyEquals_ThenReturnsLambda(DummyEntity entity)
    {
        var guid = Guid.Parse("03F04946-2243-4C1F-9F61-75EA0B7E7767");
        var query = $"where[x.Id=={guid}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Id == {guid})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Id == guid);
    }

    [Theory]
    [AutoData]
    public void GetFilter_WhenBoolUsedAsFlag_ThenReturnsLambda(DummyEntity entity)
    {
        const string query = "where[x.Flag]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be("Param_0 => Param_0.Flag");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Flag);
    }

    [Theory]
    [InlineAutoData(false)]
    [InlineAutoData(true)]
    public void GetFilter_WhenBoolPropertyEquals_ThenReturnsLambda(bool value, DummyEntity entity)
    {
        var query = $"where[x.Flag=={value}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual = (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Flag == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Flag == value);
    }

    [Theory]
    [InlineAutoData(false)]
    [InlineAutoData(true)]
    [InlineAutoData(null)]
    public void GetFilter_WhenNullableBoolPropertyEquals_ThenReturnsLambda(bool? value, DummyEntity entity)
    {
        var @null = value is null ? "null" : value.ToString();
        var query = $"where[x.NullableFlag=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableFlag == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableFlag == value);
    }

    [Theory]
    [InlineAutoData(byte.MinValue)]
    [InlineAutoData(byte.MaxValue)]
    [InlineAutoData(1)]
    [InlineAutoData(10)]
    [InlineAutoData(100)]
    [InlineAutoData(233)]
    public void GetFilter_WhenBytePropertyEquals_ThenReturnsLambda(byte value, DummyEntity entity)
    {
        var query = $"where[x.Byte=={value}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Byte == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Byte == value);
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData(byte.MinValue)]
    [InlineAutoData(byte.MaxValue)]
    [InlineAutoData((byte)1)]
    [InlineAutoData((byte)10)]
    [InlineAutoData((byte)100)]
    [InlineAutoData((byte)233)]
    public void GetFilter_WhenNullableBytePropertyEquals_ThenReturnsLambda(byte? value, DummyEntity entity)
    {
        var @null = value is null ? "null" : value.ToString();
        var query = $"where[x.NullableByte=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableByte == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableByte == value);
    }

    [Theory]
    [InlineAutoData(short.MinValue)]
    [InlineAutoData(short.MaxValue)]
    [InlineAutoData(-1000)]
    [InlineAutoData(0)]
    [InlineAutoData(1)]
    [InlineAutoData(10)]
    [InlineAutoData(100)]
    [InlineAutoData(233)]
    [InlineAutoData(32323)]
    public void GetFilter_WhenShortPropertyEquals_ThenReturnsLambda(short value, DummyEntity entity)
    {
        var query = $"where[x.Short=={value}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Short == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Short == value);
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData(short.MinValue)]
    [InlineAutoData(short.MaxValue)]
    [InlineAutoData(-1000)]
    [InlineAutoData(0)]
    [InlineAutoData(1)]
    [InlineAutoData(10)]
    [InlineAutoData(100)]
    [InlineAutoData(233)]
    [InlineAutoData(32323)]
    public void GetFilter_WhenNullableShortPropertyEquals_ThenReturnsLambda(string? stringValue, DummyEntity entity)
    {
        short? value = stringValue == null ? null : short.Parse(stringValue);
        var @null = value is null ? "null" : value.ToString();
        var query = $"where[x.NullableShort=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableShort == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableShort == value);
    }

    [Theory]
    [InlineAutoData(int.MinValue)]
    [InlineAutoData(int.MaxValue)]
    [InlineAutoData(-1)]
    [InlineAutoData(0)]
    [InlineAutoData(1)]
    [InlineAutoData(10)]
    [InlineAutoData(1000)]
    [InlineAutoData(323123)]
    public void GetFilter_WhenIntPropertyEquals_ThenReturnsLambda(int value, DummyEntity entity)
    {
        var query = $"where[x.Int=={value}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Int == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Int == value);
    }

    [Theory]
    [InlineAutoData(int.MinValue)]
    [InlineAutoData(int.MaxValue)]
    [InlineAutoData(-1)]
    [InlineAutoData(0)]
    [InlineAutoData(1)]
    [InlineAutoData(10)]
    [InlineAutoData(1000)]
    [InlineAutoData(323123)]
    [InlineAutoData(null)]
    public void GetFilter_WhenNullableIntPropertyEquals_ThenReturnsLambda(int? value, DummyEntity entity)
    {
        var @null = value is null ? "null" : value.ToString();
        var query = $"where[x.NullableInt=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableInt == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableInt == value);
    }

    [Theory]
    [InlineAutoData(long.MinValue)]
    [InlineAutoData(long.MaxValue)]
    [InlineAutoData(-1)]
    [InlineAutoData(0)]
    [InlineAutoData(1)]
    [InlineAutoData(10)]
    [InlineAutoData(1000)]
    [InlineAutoData(323123)]
    [InlineAutoData(323123321312)]
    public void GetFilter_WhenLongPropertyEquals_ThenReturnsLambda(long value, DummyEntity entity)
    {
        var query = $"where[x.Long=={value}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Long == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Long == value);
    }

    [Theory]
    [InlineAutoData(long.MinValue)]
    [InlineAutoData(long.MaxValue)]
    [InlineAutoData(-1L)]
    [InlineAutoData(0L)]
    [InlineAutoData(1L)]
    [InlineAutoData(10L)]
    [InlineAutoData(1000L)]
    [InlineAutoData(323123L)]
    [InlineAutoData(323123321312L)]
    [InlineAutoData(null)]
    public void GetFilter_WhenNullableLongPropertyEquals_ThenReturnsLambda(long? value, DummyEntity entity)
    {
        var @null = value is null ? "null" : value.ToString();
        var query = $"where[x.NullableLong=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableLong == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableLong == value);
    }

    [Theory]
    [InlineAutoData(0)]
    [InlineAutoData(1)]
    [InlineAutoData(2)]
    [InlineAutoData(30)]
    [InlineAutoData(-2)]
    [InlineAutoData(DummyEnum.Zero)]
    [InlineAutoData(DummyEnum.One)]
    [InlineAutoData(DummyEnum.Two)]
    [InlineAutoData(DummyEnum.Longer)]
    [InlineAutoData(DummyEnum.Negative)]
    public void GetFilter_WhenEnumPropertyEquals_ThenReturnsLambda(DummyEnum value, DummyEntity entity)
    {
        var query = $"where[x.Enum=={value}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Enum == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Enum == value);
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData((DummyEnum)0)]
    [InlineAutoData((DummyEnum)1)]
    [InlineAutoData((DummyEnum)2)]
    [InlineAutoData((DummyEnum)30)]
    [InlineAutoData((DummyEnum)(-2))]
    [InlineAutoData(DummyEnum.Zero)]
    [InlineAutoData(DummyEnum.One)]
    [InlineAutoData(DummyEnum.Two)]
    [InlineAutoData(DummyEnum.Longer)]
    [InlineAutoData(DummyEnum.Negative)]
    public void GetFilter_WhenNullableEnumPropertyEquals_ThenReturnsLambda(DummyEnum? value, DummyEntity entity)
    {
        var @null = value is null ? "null" : value.ToString();
        var query = $"where[x.NullableEnum=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Be($"Param_0 => (Param_0.NullableEnum == {(value is null ? @null : value)})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableEnum == value);
    }

    [Theory]
    [InlineAutoData(long.MaxValue)]
    [InlineAutoData(long.MinValue)]
    [InlineAutoData(-1L)]
    [InlineAutoData(0L)]
    [InlineAutoData(1L)]
    [InlineAutoData(10L)]
    [InlineAutoData(1000L)]
    [InlineAutoData(323123L)]
    [InlineAutoData(323123321312L)]
    public void GetFilter_WhenDecimalPropertyEquals_ThenReturnsLambda(long lValue, DummyEntity entity)
    {
        var value = (decimal)lValue;
        var query = $"where[x.Decimal=={value}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Decimal == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Decimal == value);
    }

    [Theory]
    [InlineAutoData(long.MaxValue)]
    [InlineAutoData(long.MinValue)]
    [InlineAutoData(-1L)]
    [InlineAutoData(0L)]
    [InlineAutoData(1L)]
    [InlineAutoData(10L)]
    [InlineAutoData(1000L)]
    [InlineAutoData(323123L)]
    [InlineAutoData(323123321312L)]
    [InlineAutoData(null)]
    public void GetFilter_WhenNullableDecimalPropertyEquals_ThenReturnsLambda(long? lValue, DummyEntity entity)
    {
        var value = (decimal?)lValue;
        var @null = value is null ? "null" : value.ToString();
        var query = $"where[x.NullableDecimal=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableDecimal == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableDecimal == value);
    }

    [Theory]
    [InlineAutoData(double.MaxValue)]
    [InlineAutoData(double.MinValue)]
    [InlineAutoData(-1.0)]
    [InlineAutoData(0.0)]
    [InlineAutoData(1.21)]
    [InlineAutoData(10.32132312)]
    [InlineAutoData(1000.3213213)]
    [InlineAutoData(323123.897979)]
    [InlineAutoData(323123321312.897878d)]
    public void GetFilter_WhenDoublePropertyEquals_ThenReturnsLambda(double value, DummyEntity entity)
    {
        var query = $"where[x.Double=={value.ToString()}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Double == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Double == value);
    }

    [Theory]
    [InlineAutoData(double.MaxValue)]
    [InlineAutoData(double.MinValue)]
    [InlineAutoData(-1.0)]
    [InlineAutoData(0.0)]
    [InlineAutoData(1.21)]
    [InlineAutoData(10.32132312)]
    [InlineAutoData(1000.3213213)]
    [InlineAutoData(323123.897979)]
    [InlineAutoData(323123321312.897878d)]
    [InlineAutoData(null)]
    public void GetFilter_WhenNullableDoublePropertyEquals_ThenReturnsLambda(double? value, DummyEntity entity)
    {
        var query = $"where[x.NullableDouble=={(value is null ? "null" : value.Value.ToString())}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Be($"Param_0 => (Param_0.NullableDouble == {(value is null ? "null" : value.ToString())})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableDouble == value);
    }

    [Theory]
    [InlineAutoData(float.MaxValue)]
    [InlineAutoData(float.MinValue)]
    [InlineAutoData(-1.0)]
    [InlineAutoData(0.0)]
    [InlineAutoData(1.21)]
    [InlineAutoData(10.32132312)]
    [InlineAutoData(1000.3213213)]
    [InlineAutoData(323123.897979)]
    [InlineAutoData(323123321312.897878d)]
    public void GetFilter_WhenFloatPropertyEquals_ThenReturnsLambda(float value, DummyEntity entity)
    {
        var query = $"where[x.Float=={value.ToString()}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Float == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Float == value);
    }

    [Theory]
    [InlineAutoData(float.MaxValue)]
    [InlineAutoData(float.MinValue)]
    [InlineAutoData(-1.0)]
    [InlineAutoData(0.0)]
    [InlineAutoData(1.21)]
    [InlineAutoData(10.32132312)]
    [InlineAutoData(1000.3213213)]
    [InlineAutoData(323123.897979)]
    [InlineAutoData(323123321312.897878d)]
    [InlineAutoData(null)]
    public void GetFilter_WhenNullableFloatPropertyEquals_ThenReturnsLambda(string? stringValue, DummyEntity entity)
    {
        float? value = stringValue == null ? null : float.Parse(stringValue);
        var query = $"where[x.NullableFloat=={(value is null ? "null" : value.Value.ToString())}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should()
            .Be($"Param_0 => (Param_0.NullableFloat == {(value is null ? "null" : value.ToString())})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableFloat == value);
    }

    [Theory]
    [InlineAutoData("00:00:00")]
    [InlineAutoData("23:59:59.99999999")]
    [InlineAutoData("15:13:23.000")]
    [InlineAutoData("23:59:33.999")]
    [InlineAutoData("01:45:21.321")]
    [InlineAutoData("15:55:40.433")]
    public void GetFilter_WhenTimePropertyEquals_ThenReturnsLambda(string stringValue, DummyEntity entity)
    {
        var value = TimeOnly.Parse(stringValue);
        var query = $"where[x.Time=={value.ToString()}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.Time == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.Time == value);
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData("00:00:00")]
    [InlineAutoData("23:59:59.99999999")]
    [InlineAutoData("15:13:23.000")]
    [InlineAutoData("23:59:33.999")]
    [InlineAutoData("01:45:21.321")]
    [InlineAutoData("15:55:40.433")]
    public void GetFilter_WhenNullableTimePropertyEquals_ThenReturnsLambda(string? stringValue, DummyEntity entity)
    {
        TimeOnly? value = stringValue is null ? null : TimeOnly.Parse(stringValue);
        var @null = value is not null ? value.Value.ToString() : "null";
        var query = $"where[x.NullableTime=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableTime == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableTime == value);
    }

    [Theory]
    [InlineAutoData("0001-01-01 00:00:00")]
    [InlineAutoData("9999-01-01 00:00")]
    [InlineAutoData("2000-02-27 15:13:23.000")]
    [InlineAutoData("2024-10-11 23:59:33.999")]
    [InlineAutoData("2010-08-31 01:45:21.321")]
    [InlineAutoData("2030-12-22 15:55:40.433")]
    public void GetFilter_WhenDateTimePropertyEquals_ThenReturnsLambda(DateTime value, DummyEntity entity)
    {
        var query = $"where[x.DateTime=={value.ToString(CultureInfo.InvariantCulture)}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.DateTime == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.DateTime == value);
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData("0001-01-01 00:00")]
    [InlineAutoData("9999-01-01 00:00")]
    [InlineAutoData("2000-02-27 15:13:23.000")]
    [InlineAutoData("2024-10-11 23:59:33.999")]
    [InlineAutoData("2010-08-31 01:45:21.321")]
    [InlineAutoData("2030-12-22 15:55:40.433")]
    public void GetFilter_WhenNullableDateTimePropertyEquals_ThenReturnsLambda(string? dateString, DummyEntity entity)
    {
        DateTime? value = dateString is not null ? DateTime.Parse(dateString) : null;
        var @null = value is not null ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";
        var query = $"where[x.NullableDateTime=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableDateTime == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableDateTime == value);
    }

    [Theory]
    [InlineAutoData("0001-01-01 00:00+00:00")]
    [InlineAutoData("9999-01-01 00:00")]
    [InlineAutoData("2000-02-27 15:13:23.000")]
    [InlineAutoData("2024-10-11 23:59:33.999")]
    [InlineAutoData("2010-08-31 01:45:21.321")]
    [InlineAutoData("2030-12-22 15:55:40.433")]
    [InlineAutoData("2024-05-31 01:45:21.321+00:00")]
    [InlineAutoData("2011-01-30 15:55:20.433+01:00")]
    [InlineAutoData("2023-07-19 01:45:21.321-01:30")]
    [InlineAutoData("2012-12-31 15:55:32.433+04:00")]
    public void GetFilter_WhenDateTimeOffsetPropertyEquals_ThenReturnsLambda(DateTimeOffset value, DummyEntity entity)
    {
        var query = $"where[x.DateTimeOffset=={value.ToString()}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.DateTimeOffset == {value})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.DateTimeOffset == value);
    }

    [Theory]
    [InlineAutoData(null)]
    [InlineAutoData("0001-01-01 00:00+00:00")]
    [InlineAutoData("9999-01-01 00:00")]
    [InlineAutoData("2000-02-27 15:13:23.000")]
    [InlineAutoData("2024-10-11 23:59:33.999")]
    [InlineAutoData("2010-08-31 01:45:21.321")]
    [InlineAutoData("2030-12-22 15:55:30.433")]
    [InlineAutoData("2024-05-31 01:45:21.321+00:00")]
    [InlineAutoData("2011-01-30 15:55:20.433+01:00")]
    [InlineAutoData("2023-07-19 01:45:21.321-01:30")]
    [InlineAutoData("2012-12-31 15:55:32.433+04:00")]
    public void GetFilter_WhenNullableDateTimeOffsetPropertyEquals_ThenReturnsLambda(string? dateString,
        DummyEntity entity)
    {
        DateTimeOffset? value = dateString is not null ? DateTimeOffset.Parse(dateString) : null;
        var @null = value is not null ? value.Value.ToString() : "null";
        var query = $"where[x.NullableDateTimeOffset=={@null}]";
        var tokens = _tokenizer.Tokenize(query);
        ITreeBranch tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);
        var actual =
            (Expression<Func<DummyEntity, bool>>)_sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(),
                query);
        actual.Should().NotBeNull();
        actual.ToString().Should().Be($"Param_0 => (Param_0.NullableDateTimeOffset == {@null})");
        var filter = actual.Compile();
        filter.Invoke(entity).Should().Be(entity.NullableDateTimeOffset == value);
    }
}