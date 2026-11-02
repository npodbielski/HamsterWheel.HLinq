using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedTreeElement;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Parser;

/// <summary>
/// The tree a call in select parses into: a call is a member of the assignment it is written as,
/// whether it hangs on a property path or stands on nothing - the parser only says there is a
/// call, which kind of call it is stays the builder's question.
/// </summary>
public class HLinqParserSelectCallUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Fact]
    public void Parse_WhenRenamedStaticCall_ThenCallIsAMemberBesideItsRename()
    {
        const string query = "select[R=reverse(x.Name)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("R"),
                    MethodElement(
                        MethodName("reverse"),
                        Property(Entity(), Dot, Prop("Name"))
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenUnnamedStaticCall_ThenCallIsTheMemberAlone()
    {
        const string query = "select[reverse(x.Name)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    MethodElement(
                        MethodName("reverse"),
                        Property(Entity(), Dot, Prop("Name"))
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenRenamedPropertyCall_ThenCallHangsOnThePropertyItNames()
    {
        const string query = "select[U=x.Name.ToUpper()]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("U"),
                    Property(Assignment, Entity(), Dot, Prop("Name")),
                    MethodCallWithoutArguments("ToUpper")
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenCallWithWrittenArguments_ThenArgumentsAreTheCallsChildren()
    {
        const string query = "select[R=x.Name.Substring(0,2)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("R"),
                    Property(Assignment, Entity(), Dot, Prop("Name")),
                    MethodElement(
                        MethodName("Substring"),
                        MethodConstParam(NameOrValue("0"), Comma),
                        MethodConstParam(NameOrValue("2"))
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenStaticCallInsideNestedSelect_ThenCallStandsInTheInnerAssignment()
    {
        const string query = "select[A=x.Addresses.select(City=upper(x.Name))]";
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
                            MethodElement(
                                MethodName("upper"),
                                Property(Entity(), Dot, Prop("Name"))
                            )
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenPropertyCallInsideNestedSelect_ThenCallHangsOnWhicheverPathItNames()
    {
        const string query = "select[A=x.Addresses.select(City=x.Name.ToUpper())]";
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
                            Property(Assignment, Entity(), Dot, Prop("Name")),
                            MethodCallWithoutArguments("ToUpper")
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenInnerCallHangsOnInnerMember_ThenPathIsTheInnerOne()
    {
        const string query = "select[A=x.Addresses.select(City=a.City.ToUpper())]";
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
                            MethodCallWithoutArguments("ToUpper")
                        )
                    )
                ))
        ]);
    }
}
