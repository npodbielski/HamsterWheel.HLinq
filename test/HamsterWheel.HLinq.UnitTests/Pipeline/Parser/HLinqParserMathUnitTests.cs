using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedTreeElement;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Parser;

/// <summary>
/// One operator is one element holding the value written after it.
/// </summary>
public class HLinqParserMathUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Fact]
    public void Parse_WhenRenamedJoin_ThenFirstValueAndEveryOperatorAreMembersOfTheAssignment()
    {
        const string query = "select[FullName=x.Name+\" \"+x.Surname]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("FullName"),
                    Property(Assignment, Entity(), Dot, Prop("Name")),
                    MathTerm(Plus,
                        MathText(" ")
                    ),
                    MathTerm(Plus,
                        Property(Entity(), Dot, Prop("Surname"))
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenExpressionHangsOnACall_ThenTheCallIsAValueBesideTheOperator()
    {
        const string query = "select[U=x.Name.ToUpper()+\"!\"]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("U"),
                    Property(Assignment, Entity(), Dot, Prop("Name")),
                    MethodCallWithoutArguments("ToUpper"),
                    MathTerm(Plus,
                        MathText("!")
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenJoinInsideNestedSelect_ThenInnerValuesParseTheSameWay()
    {
        const string query = "select[A=x.Addresses.select(City=a.City+\" \"+x.Name)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("A"),
                    Property(Assignment, Entity(), Dot, Prop("Addresses")),
                    NestedSelect(
                        PropertyAssignment(
                            InitializerPropertyName("City"),
                            InnerProperty(Assignment, SubEntity("a"), Dot, Prop("City")),
                            MathTerm(Plus,
                                MathText(" ")
                            ),
                            MathTerm(Plus,
                                Property(Entity(), Dot, Prop("Name"))
                            )
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenDifferenceComparesToValue_ThenTheRunIsOneSideOfTheComparison()
    {
        //the side a comparison reads is what answers it: a bare math run hangs on the member it
        //begins with, exactly as a call does - the member and each operator are siblings of the side
        const string query = "where[x.Int-x.Long>5]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(Entity(), Dot, Prop("Int")),
                    MathTerm(Minus,
                        Property(Entity(), Dot, Prop("Long"))
                    ),
                    ComparisonOperation(GreaterThan),
                    ComparisonConstant(NameOrValue("5"))
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenMathHangsOnACall_ThenTheCallStandsBesideTheOperator()
    {
        const string query = "where[x.Name.ToUpper()+x.Name==q]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(Entity(), Dot, Prop("Name")),
                    MethodCallWithoutArguments("ToUpper"),
                    MathTerm(Plus,
                        Property(Entity(), Dot, Prop("Name"))
                    ),
                    ComparisonOperation(Equality),
                    ComparisonConstant(NameOrValue("q"))
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenGroupStandsOnAValueSide_ThenItIsTheSideTheComparisonReads()
    {
        const string query = "where[x.Int>(2*3)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(Entity(), Dot, Prop("Int")),
                    ComparisonOperation(GreaterThan),
                    MathGroup(
                        MathNumber("2"),
                        MathTerm(Multiply,
                            MathNumber("3")
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenGroupHoldsOneWrittenValue_ThenThatValueIsTheGroup()
    {
        //`(5)` after a comparison is a group of one: the tokenizer commits the number as the free
        //value of the clause and the parser reads it as the operand it stands for
        const string query = "where[x.Int>(5)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(Entity(), Dot, Prop("Int")),
                    ComparisonOperation(GreaterThan),
                    MathGroup(
                        MathValue("5")
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenKeyIsAMathRun_ThenTheOrderingReadsItsOneValue()
    {
        const string query = "orderBy[x.Int*2]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            OrderByRoot(
                Property(Entity(), Dot, Prop("Int")),
                MathTerm(Multiply,
                    MathNumber("2")
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenKeyOpensWithAGroup_ThenTheOrderingStillReadsOneValue()
    {
        const string query = "orderBy[(x.Int+x.Long)*2]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            OrderByRoot(
                MathGroup(
                    Property(Entity(), Dot, Prop("Int")),
                    MathTerm(Plus,
                        Property(Entity(), Dot, Prop("Long"))
                    )
                ),
                MathTerm(Multiply,
                    MathNumber("2")
                ))
        ]);
    }
}
