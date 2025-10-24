using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.UnitTests.Assertions;
using HamsterWheel.HLinq.UnitTests.Dummies;
using static HamsterWheel.HLinq.UnitTests.Assertions.ExpectedTreeElement;

namespace HamsterWheel.HLinq.UnitTests;

public class HLinqParserUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Fact]
    public void Parse_WhenSingleRename_ThenCanParse()
    {
        const string query = "select[x.name]";
        IToken[] tokens =
        [
            new Select(),
            new LeftSquareBracket(),
            new Entity(),
            new Dot(),
            new PropertyAccess(),
            new RightSquareBracket()
        ];

        var tree =_sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    Property(ExpectedToken.Entity(), ExpectedToken.Dot, ExpectedToken.Prop(""))
                )
            )
        ]);
    }

    [Fact]
    public void Parse_WhenPropertyWithConstValue_ThenCanParse()
    {
        const string query = "select[Directory=Core]";
        IToken[] tokens =
        [
            new Select(),
            new LeftSquareBracket(),
            new NameOrValue(),
            new Assignment(),
            new NameOrValue(),
            new RightSquareBracket()
        ];

        var tree =_sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("Directory"),
                    InitializerConstantValue("Core")
                )
            )
        ]);
    }

    [Fact]
    public void Parse_WhenSourcePropertyAndPropertyWithConstValue_ThenCanParse()
    {
        const string query = "select[x.Id,Directory=Core]";
        IToken[] tokens =
        [
            new Select(),
            new LeftSquareBracket(),
            new Entity(),
            new Dot(),
            new PropertyAccess(),
            new Comma(),
            new NameOrValue(),
            new Assignment(),
            new NameOrValue(),
            new RightSquareBracket()
        ];

        var tree =_sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    Property(
                        ExpectedToken.Entity(),
                        ExpectedToken.Dot,
                        ExpectedToken.Prop("Id")
                    )),
                PropertyAssignment(
                    InitializerPropertyName("Directory"),
                    InitializerConstantValue("Core")
                )
            )
        ]);
    }

    [Fact]
    public void Parse_WhenValidAndPropEqual_ThenCanParse()
    {
        const string query = "where[x.Name.Contains(test)]";
        IToken[] tokens =
        [
            new Where(), new LeftSquareBracket(), new Entity(),
            new Dot(), new PropertyAccess(), new Dot(), new MethodCall(),
            new LeftCircleBracket(), new NameOrValue(), new RightCircleBracket(),
            new RightSquareBracket()
        ];
        var tree =_sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(
                        ExpectedToken.Entity(),
                        ExpectedToken.Dot,
                        ExpectedToken.Prop("Name")),
                    MethodElement(
                        MethodConstParam(ExpectedToken.NameOrValue("test"))
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenStaticMethodCallWithPropAndConstant_ThenCanParse()
    {
        const string query = "where[ilike(x.Name, test)]";
        IToken[] tokens =
        [
            new Where(),
            new LeftSquareBracket(),
            new MethodCall(),
            new LeftCircleBracket(),
            new Entity(),
            new Dot(),
            new PropertyAccess(),
            new Comma(),
            new NameOrValue(),
            new RightCircleBracket(),
            new RightSquareBracket()
        ];
        var tree =_sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    MethodElement(
                        Property(
                            ExpectedToken.Entity(""),
                            ExpectedToken.Dot,
                            ExpectedToken.Prop("")
                        ),
                        MethodConstParam(
                            ExpectedToken.Comma,
                            ExpectedToken.NameOrValue("")
                        )
                    )
                )
            )
        ]);
    }

    [Fact]
    public void Parse_WhenTwoConditions_ThenCanParse()
    {
        const string query = "where[x.Name.Contains(test)&&x.Id==77774169-BB9D-4DF9-A4A7-52019C4A445D]";
        var tokens = new HLinqTokenizer(TestServicesCollection.TokenPossibilities).Tokenize(query);

        var tree =_sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(
                        ExpectedToken.Entity(),
                        ExpectedToken.Dot,
                        ExpectedToken.Prop("Name")),
                    MethodElement(
                        MethodConstParam(ExpectedToken.NameOrValue("test"))
                    )
                ),
                ConditionElement(
                    Property(
                        ExpectedToken.Entity(),
                        ExpectedToken.Dot,
                        ExpectedToken.Prop("Id")),
                    ComparisonOperation(ExpectedToken.Equality),
                    ComparisonConstant(ExpectedToken.NameOrValue("77774169-BB9D-4DF9-A4A7-52019C4A445D"))
                )
            )
        ]);
    }
}