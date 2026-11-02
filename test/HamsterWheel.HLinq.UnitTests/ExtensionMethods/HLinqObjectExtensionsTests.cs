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
        actual.Should().Be(expected);
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
    public void ExecuteHLinq_WhenOnObjectWithQuotedValue_ThenIsValid()
    {
        //arrange
        var obj = new
        {
            Name = "test.text",
            Contents = "this is file content"
        };
        var expected = new
        {
            FileName = "test.text",
            Contents = "this is file content",
            Directory = "/temp"
        };

        //act
        var actual = obj.ExecuteHLinq("FileName=x.Name, x.Contents, Directory=\"/temp\"");

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

    [Fact]
    public void ExecuteHLinq_WhenGroupBy_ThenReturnsGroupsOfKeyAndRows()
    {
        //arrange
        var rows = GroupedRows();

        //act
        var actual = (object[])rows.ExecuteHLinq("groupBy[x.Enum]");

        //assert - every group answers its key and the rows standing under it
        actual.Should().HaveCount(3);
        RowsOf(actual, RandomEnum.One).Length.Should().Be(2);
        RowsOf(actual, RandomEnum.Two).Length.Should().Be(1);
        RowsOf(actual, RandomEnum.Longer).Length.Should().Be(1);
        KeyOf(actual[0]).Should().Be(RandomEnum.One);
    }

    [Fact]
    public void ExecuteHLinq_WhenGroupByThenWhereCount_ThenKeepsOnlyTheBiggerGroups()
    {
        //arrange
        var rows = GroupedRows();

        //act
        var actual = (object[])rows.ExecuteHLinq("groupBy[x.Enum].where[x.Count()>1]");

        //assert
        actual.Should().HaveCount(1);
        KeyOf(actual[0]).Should().Be(RandomEnum.One);
    }

    [Fact]
    public void ExecuteHLinq_WhenGroupByThenSelectKeyAndCount_ThenAnswersCountBesideKeyAndRows()
    {
        //arrange
        var rows = GroupedRows();

        //act
        var actual = (object[])rows.ExecuteHLinq("groupBy[x.Enum].select[C=x.Count()]");

        //assert
        ValueOf(actual, RandomEnum.One, "C").Should().Be(2);
        ValueOf(actual, RandomEnum.Two, "C").Should().Be(1);
        ValueOf(actual, RandomEnum.One, "Rows").Should().BeAssignableTo<System.Collections.IEnumerable>();
    }

    [Fact]
    public void ExecuteHLinq_WhenGroupByThenSelectNamedKeyAndCount_ThenNamedKeyShapesTheRows()
    {
        //arrange
        var rows = GroupedRows();

        //act - K names a row member so it shapes the rows of the group, while the count rides
        //beside Key and Rows of the group itself
        var actual = (object[])rows.ExecuteHLinq("groupBy[x.Enum].select[K=x.Enum,C=x.Count()]");

        //assert
        ValueOf(actual, RandomEnum.One, "C").Should().Be(2);
        var firstRow = RowsOf(actual, RandomEnum.One)[0];
        firstRow.GetType().GetProperty("K")!.GetValue(firstRow).Should().Be(RandomEnum.One);
    }

    [Fact]
    public void ExecuteHLinq_WhenGroupByThenSelectEveryAggregate_ThenEachAnswersItsValue()
    {
        //arrange
        var rows = GroupedRows();

        //act
        var actual = (object[])rows.ExecuteHLinq(
            "groupBy[x.Enum].select[C=x.Count(),S=x.Sum(x.Int),A=x.Average(x.Int),Mi=x.Min(x.Int),M=x.Max(x.Int)]");

        //assert
        var one = ValueOf(actual, RandomEnum.One, "C");
        one.Should().Be(2);
        ValueOf(actual, RandomEnum.One, "S").Should().Be(40);
        ValueOf(actual, RandomEnum.One, "A").Should().Be(20.0);
        ValueOf(actual, RandomEnum.One, "Mi").Should().Be(10);
        ValueOf(actual, RandomEnum.One, "M").Should().Be(30);
    }

    [Fact]
    public void ExecuteHLinq_WhenGroupByThenWhereCountThenOrderByCount_ThenOrdersTheFilteredGroups()
    {
        //arrange
        var rows = GroupedRows();

        //act
        var actual = (object[])rows.ExecuteHLinq(
            "groupBy[x.Enum].where[x.Count()>0].orderBy[x.Max(x.Int)]");

        //assert - One (max 30) before Two (40) before Longer (50)
        actual.Select(g => KeyOf(g)).Should().ContainInOrder(RandomEnum.One, RandomEnum.Two, RandomEnum.Longer);
    }

    //three groups, the One group carries two rows with their own numbers to aggregate over
    private static GroupedRow[] GroupedRows() =>
    [
        new(RandomEnum.One) { Int = 10 },
        new(RandomEnum.One) { Int = 30 },
        new(RandomEnum.Two) { Int = 40 },
        new(RandomEnum.Longer) { Int = 50 },
    ];

    private class GroupedRow(RandomEnum @enum)
    {
        public RandomEnum Enum { get; set; } = @enum;
        public int Int { get; set; }
    }

    private static object KeyOf(object group) =>
        group.GetType().GetProperty("Key")!.GetValue(group)!;

    private static object[] RowsOf(object[] groups, RandomEnum key) =>
        [.. ((System.Collections.IEnumerable)ValueOf(groups, key, "Rows")).Cast<object>()];

    private static object ValueOf(object[] groups, RandomEnum key, string name)
    {
        var group = groups.Single(g => KeyOf(g).Equals(key));
        return group.GetType().GetProperty(name)!.GetValue(group)!;
    }
}