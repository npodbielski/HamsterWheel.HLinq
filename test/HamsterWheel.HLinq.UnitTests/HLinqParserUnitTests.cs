using FluentAssertions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filter;
using HamsterWheel.HLinq.Tokens.Select;
using HamsterWheel.HLinq.Tree.Filter;
using HamsterWheel.HLinq.UnitTests.Assertions;
using HamsterWheel.HLinq.UnitTests.Dummies;
using static HamsterWheel.HLinq.UnitTests.Assertions.ExpectedTreeElement;
using Property = HamsterWheel.HLinq.Tree.Property;

namespace HamsterWheel.HLinq.UnitTests;

public class HLinqParserUnitTests
{
    private readonly IHLinqParsersCollection _parsersCollection = new HLinqServicesCollection(new HLinqCore());
    private readonly HLinqParser _sut;

    public HLinqParserUnitTests() => _sut = new HLinqParser(_parsersCollection);


    [Fact]
    public void Parse_WhenSingleRename_ThenCanParse()
    {
        const string query = "select[x.name]";
        TokenBase[] tokens =
        [
            new Select(default),
            new LeftSquareBracket(default),
            new Entity(default),
            new Dot(default),
            new PropertyAccess(default),
            new RightSquareBracket(default)
        ];

        ITreeBranch tree = _sut.Parse<DummyEntity>(tokens, query);

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
        TokenBase[] tokens =
        [
            new Select(default),
            new LeftSquareBracket(default),
            new NameOrValue(default),
            new Assignment(default),
            new NameOrValue(default),
            new RightSquareBracket(default)
        ];

        ITreeBranch tree = _sut.Parse<DummyEntity>(tokens, query);

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
        TokenBase[] tokens =
        [
            new Select(default),
            new LeftSquareBracket(default),
            new Entity(default),
            new Dot(default),
            new PropertyAccess(default),
            new Comma(default),
            new NameOrValue(default),
            new Assignment(default),
            new NameOrValue(default),
            new RightSquareBracket(default)
        ];

        ITreeBranch tree = _sut.Parse<DummyEntity>(tokens, query);

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
        TokenBase[] tokens =
        [
            new Where(default), new LeftSquareBracket(default), new Entity(default),
            new Dot(default), new PropertyAccess(default), new Dot(default), new MethodCall(default),
            new LeftCircleBracket(default), new NameOrValue(default), new RightCircleBracket(default),
            new RightSquareBracket(default)
        ];
        ITreeBranch tree = _sut.Parse<DummyEntity>(tokens, query);

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
        TokenBase[] tokens =
        [
            new Where(default),
            new LeftSquareBracket(default),
            new MethodCall(default),
            new LeftCircleBracket(default),
            new Entity(default),
            new Dot(default),
            new PropertyAccess(default),
            new Comma(default),
            new NameOrValue(default),
            new RightCircleBracket(default),
            new RightSquareBracket(default)
        ];
        ITreeBranch tree = _sut.Parse<DummyEntity>(tokens, query);
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
        var tokens = new HLinqTokenizer(new HLinqServicesCollection(new HLinqCore())).Tokenize(query);

        ITreeBranch tree = _sut.Parse<DummyEntity>(tokens, query);

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