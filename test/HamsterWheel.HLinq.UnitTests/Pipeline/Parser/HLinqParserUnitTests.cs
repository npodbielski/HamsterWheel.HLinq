using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Assignment = HamsterWheel.HLinq.Tokens.Selecting.Assignment;
using Comma = HamsterWheel.HLinq.Tokens.Filtering.Comma;
using Dot = HamsterWheel.HLinq.Tokens.Dot;
using Entity = HamsterWheel.HLinq.Tokens.Entity;
using LeftCircleBracket = HamsterWheel.HLinq.Tokens.Filtering.LeftCircleBracket;
using LeftSquareBracket = HamsterWheel.HLinq.Tokens.LeftSquareBracket;
using MethodName = HamsterWheel.HLinq.Tokens.Filtering.MethodName;
using NameOrValue = HamsterWheel.HLinq.Tokens.NameOrValue;
using OrderBy = HamsterWheel.HLinq.Tokens.Ordering.OrderBy;
using OrderByDescending = HamsterWheel.HLinq.Tokens.Ordering.OrderByDescending;
using RightCircleBracket = HamsterWheel.HLinq.Tokens.Filtering.RightCircleBracket;
using RightSquareBracket = HamsterWheel.HLinq.Tokens.RightSquareBracket;
using Select = HamsterWheel.HLinq.Tokens.Selecting.Select;
using Skip = HamsterWheel.HLinq.Tokens.Paging.Skip;
using Take = HamsterWheel.HLinq.Tokens.Paging.Take;
using ThenBy = HamsterWheel.HLinq.Tokens.Ordering.ThenBy;
using ThenByDescending = HamsterWheel.HLinq.Tokens.Ordering.ThenByDescending;
using Where = HamsterWheel.HLinq.Tokens.Filtering.Where;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedTreeElement;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Parser;

public class HLinqParserUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Fact]
    public void Parse_WhenEmptySelect_ThenThrows()
    {
        const string query = "select[]";
        IToken[] tokens =
        [
            Select.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenEmptyWhere_ThenDoNotThrows()
    {
        const string query = "where[]";
        IToken[] tokens =
        [
            Where.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().NotThrow();
    }

    [Fact]
    public void Parse_WhenEmptySkip_ThenThrows()
    {
        const string query = "skip[]";
        IToken[] tokens =
        [
            Skip.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenEmptyTake_ThenThrows()
    {
        const string query = "take[]";
        IToken[] tokens =
        [
            Take.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenEmptyThenBy_ThenThrows()
    {
        const string query = "thenBy[]";
        IToken[] tokens =
        [
            ThenBy.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenEmptyThenByDescending_ThenThrows()
    {
        const string query = "thenByDescending[]";
        IToken[] tokens =
        [
            ThenByDescending.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenEmptyOrderBy_ThenThrows()
    {
        const string query = "orderBy[]";
        IToken[] tokens =
        [
            OrderBy.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenEmptyOrderByDescending_ThenThrows()
    {
        const string query = "orderByDescending[]";
        IToken[] tokens =
        [
            OrderByDescending.Empty,
            LeftSquareBracket.Empty,
            RightSquareBracket.Empty
        ];

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenSingleRename_ThenCanParse()
    {
        const string query = "select[x.name]";
        IToken[] tokens =
        [
            Select.Empty,
            LeftSquareBracket.Empty,
            Entity.Empty,
            Dot.Empty,
            PropertyName.Empty,
            RightSquareBracket.Empty
        ];

        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    Property(Entity(), ExpectedToken.Dot, Prop(""))
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
            Select.Empty,
            LeftSquareBracket.Empty,
            NameOrValue.Empty,
            Assignment.Empty,
            NameOrValue.Empty,
            RightSquareBracket.Empty
        ];

        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

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
            Select.Empty,
            LeftSquareBracket.Empty,
            Entity.Empty,
            Dot.Empty,
            PropertyName.Empty,
            Comma.Empty,
            NameOrValue.Empty,
            Assignment.Empty,
            NameOrValue.Empty,
            RightSquareBracket.Empty
        ];

        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    Property(
                        Entity(),
                        ExpectedToken.Dot,
                        Prop("Id")
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
            Where.Empty, LeftSquareBracket.Empty, Entity.Empty,
            Dot.Empty, PropertyName.Empty, Dot.Empty, MethodName.Empty,
            LeftCircleBracket.Empty, NameOrValue.Empty, RightCircleBracket.Empty,
            RightSquareBracket.Empty
        ];
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(
                        Entity(),
                        ExpectedToken.Dot,
                        Prop("Name")),
                    MethodElement(
                        MethodName("Contains"),
                        MethodConstParam(NameOrValue("test"))
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
            Where.Empty,
            LeftSquareBracket.Empty,
            MethodName.Empty,
            LeftCircleBracket.Empty,
            Entity.Empty,
            Dot.Empty,
            PropertyName.Empty,
            Comma.Empty,
            NameOrValue.Empty,
            RightCircleBracket.Empty,
            RightSquareBracket.Empty
        ];
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);
        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    MethodElement(
                        MethodName("ilike"),
                        Property(
                            Entity(""),
                            ExpectedToken.Dot,
                            Prop("")
                        ),
                        MethodConstParam(
                            ExpectedToken.Comma,
                            NameOrValue("")
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

        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionElement(
                    Property(
                        Entity(),
                        ExpectedToken.Dot,
                        Prop("Name")),
                    MethodElement(
                        MethodName("Contains"),
                        MethodConstParam(NameOrValue("test"))
                    )
                ),
                ConditionElement(
                    Property(
                        Entity(),
                        ExpectedToken.Dot,
                        Prop("Id")),
                    ComparisonOperation(ExpectedToken.Equality),
                    ComparisonConstant(NameOrValue("77774169-BB9D-4DF9-A4A7-52019C4A445D"))
                )
            )
        ]);
    }

    [Fact]
    public void Parse_WhenTwoConditionGroups_ThenCanParse()
    {
        const string query = "where[(x.Age > 0 && x.Age < 100)||(x.Name.Contains(test))]";
        var tokens = new HLinqTokenizer(TestServicesCollection.TokenPossibilities).Tokenize(query);

        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            WhereRoot(
                ConditionGroup(
                    ConditionElement(
                        Property(
                            Entity(),
                            ExpectedToken.Dot,
                            Prop("Age")),
                        ComparisonOperation(ExpectedToken.GreaterThan),
                        ComparisonConstant(NameOrValue("0"))
                    ),
                    ConditionElement(
                        Property(
                            Entity(),
                            ExpectedToken.Dot,
                            Prop("Age")),
                        ComparisonOperation(ExpectedToken.LessThan),
                        ComparisonConstant(NameOrValue("100"))
                    )
                ),
                ConditionGroup(
                    ExpectedToken.Or,
                    ConditionElement(
                        Property(
                            Entity(),
                            ExpectedToken.Dot,
                            Prop("Id")),
                        MethodElement(
                            MethodName("Contains"),
                            MethodConstParam(NameOrValue("test"))
                        )
                    )
                )
            )
        ]);
    }
}