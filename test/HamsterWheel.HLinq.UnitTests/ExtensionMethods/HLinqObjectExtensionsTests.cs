using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.UnitTests;

public class HLinqObjectExtensionsTests
{
    [Fact]
    public void ExecuteHLinq_WhenSelectOnSingleObject_ThenReturnsCorrectData()
    {
        //arrange
        const string expected = "aa";
        var obj = new
        {
            Name = expected
        };

        //act
        var actual = obj.ExecuteHLinq("select[x.Name]");

        //assert
        actual.Should().NotBeNull();
        actual!.Should().Be(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenSelectOnSingleObjectWithoutSelect_ThenReturnsCorrectData()
    {
        //arrange
        const string expected = "aa";
        var obj = new
        {
            Name = expected
        };

        //act
        var actual = obj.ExecuteHLinq("x.Name");

        //assert
        actual.Should().NotBeNull();
        actual.Should().Be(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenOnCollectionWithFilter_ThenReturnsFilteredCollection()
    {
        //arrange
        var collection = RandomData.Get();
        var expected = collection.Where(c => c.Enum == RandomEnum.One);

        //act
        var actual = collection.ExecuteHLinq("where[x.Enum == One]");

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenOnCollectionWithSelectWith2Prop_ThenReturnsAllProperties()
    {
        //arrange
        var collection = RandomData.Get();
        var expected = collection.Select(c => new { c.Name, c.Enum });

        //act
        var actual = collection.ExecuteHLinq("x.Name, x.Enum");

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenOnCollectionWithSelectWith1Prop_ThenReturnsAllProperties()
    {
        //arrange
        var collection = RandomData.Get();
        var expected = collection.Select(c => c.Name);

        //act
        var actual = collection.ExecuteHLinq("x.Name");

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenOnCollectionOfPrimitives_ThenReturnsFiltered()
    {
        //arrange
        var collection = Enumerable.Range(0, 10).ToArray();
        var expected = collection.Where(c => c > 5);

        //act
        var actual = collection.ExecuteHLinq("where[x > 5]");

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenSelectForNotExistingProperty_ThenThrowsException()
    {
        //arrange
        const string propName = "Name";
        var obj = new
        {
            NoNameProperty = "test"
        };
        var action = () => obj.ExecuteHLinq($"x.{propName}");

        //act
        var actual = action.Should().Throw<InvalidPropertyPathException>();

        //assert
        actual.WithMessage($"Invalid property path '{propName}'*");
    }

    [Fact]
    public void ExecuteHLinq_WhenOnCollectionWithSelectWith2PropsEnclosedInSelect_ThenReturnsAllProperties()
    {
        //arrange
        var collection = RandomData.Get();
        var expected = collection.Select(c => new { c.Name, c.Enum });

        //act
        var actual = collection.ExecuteHLinq("select[x.Name, x.Enum]");

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenOnCollectionWithSelectWith2PropsOneFromConst_ThenReturnsAllProperties()
    {
        //arrange
        var collection = RandomData.Get();
        var expected = collection.Select(c => new { c.Name, active = "true" });

        //act
        var actual = collection.ExecuteHLinq("select[x.name, active=true]");

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenSelectWith2PropsAfterWhereFilter_ThenReturnsAllProperties()
    {
        //arrange
        var collection = RandomData.Get();
        var expected = collection.Where(x => x.Enum == RandomEnum.One).Select(c => new { c.Name, c.Enum });

        //act
        var actual = collection.ExecuteHLinq("where[x.Enum == One].select[x.Name, x.Enum]");

        //assert
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(expected);
    }
}