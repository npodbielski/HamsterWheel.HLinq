using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedTreeElement;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Parser;

/// <summary>
/// The tree a nested select parses into: the call hangs on the navigation of its member, the
/// members between the brackets are member assignments again - which is what lets a call stand
/// on a member of a call without any per-depth case.
/// </summary>
public class HLinqParserNestedSelectUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Fact]
    public void Parse_WhenNestedSelect_ThenCallHangsOnTheNavigationOfItsMember()
    {
        const string query = "select[x.Addresses.select(a.Street)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    Property(Entity(), Dot, Prop("Addresses")),
                    NestedSelect(
                        PropertyAssignment(
                            InnerProperty(SubEntity("a"), Dot, Prop("Street"))
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenRenamedNestedSelectWithOuterCapture_ThenRenameAndCaptureStandBeside()
    {
        const string query = "select[address=x.Addresses.select(contact=x.Name,a.City)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    InitializerPropertyName("address"),
                    Property(Assignment, Entity(), Dot, Prop("Addresses")),
                    NestedSelect(
                        PropertyAssignment(
                            InitializerPropertyName("contact"),
                            Property(Assignment, Entity(), Dot, Prop("Name"))
                        ),
                        PropertyAssignment(
                            InnerProperty(SubEntity("a"), Dot, Prop("City"))
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenNestedSelectUnderNestedSelect_ThenTreesNestTheSameWay()
    {
        const string query = "select[x.Addresses.select(a.Street,country=a.Country.select(c.Name))]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    Property(Entity(), Dot, Prop("Addresses")),
                    NestedSelect(
                        PropertyAssignment(
                            InnerProperty(SubEntity("a"), Dot, Prop("Street"))
                        ),
                        PropertyAssignment(
                            InitializerPropertyName("country"),
                            InnerProperty(Assignment, SubEntity("a"), Dot, Prop("Country")),
                            NestedSelect(
                                PropertyAssignment(
                                    InnerProperty(SubEntity("c"), Dot, Prop("Name"))
                                )
                            )
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenBareInnerName_ThenItIsTheElementsMember()
    {
        const string query = "select[x.Addresses.select(Street)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);
        var tree = _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        tree.Should().HaveStructureOf(query, [
            SelectRoot(
                PropertyAssignment(
                    Property(Entity(), Dot, Prop("Addresses")),
                    NestedSelect(
                        PropertyAssignment(
                            InnerProperty(NameOrValue("Street"))
                        )
                    )
                ))
        ]);
    }

    [Fact]
    public void Parse_WhenCallNamedOtherThanSelect_ThenRefused()
    {
        const string query = "select[x.Addresses.count(a.Street)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        var action = () => _sut.TestParseEntryPoint<DummyEntity>(query, tokens);

        action.Should().Throw<Exception>().Which.Message.Should().Contain("count");
    }
}
