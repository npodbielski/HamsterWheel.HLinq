using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Tree;

/// <summary>
/// The last segment of a path names a member written without a name.
/// </summary>
public class PathLastNameUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Theory]
    [InlineData("select[x.Nested.Name]", "Name")]
    [InlineData("select[x.Nested.Name,x.Text]", "Name")]
    [InlineData("select[x.Text]", "Text")]
    public void GetLastName_WhenThePathIsWrittenOnTheEntity_ThenItIsTheSegmentTheQueryEndsOn(
        string query,
        string expected)
    {
        //act
        var property = PropertyOf(query);

        //assert
        property.GetLastName(query).Should().Be(expected);
        property.GetLastName(query).Should().Be(property.GetPath(query).Last());
    }

    [Theory]
    [InlineData("select[x.Children.select(a.Name)]", "Name")]
    [InlineData("select[x.Children.select(Name)]", "Name")]
    public void GetLastName_WhenThePathIsWrittenOnTheElement_ThenItNamesTheElementMember(
        string query,
        string expected)
    {
        //act
        var inner = InnerPropertyOf(query);

        //assert
        inner.GetLastName(query).Should().Be(expected);
        inner.GetLastName(query).Should().Be(inner.GetPath(query).Last());
    }

    //the first member of the select is what the theories name, so no test reads a position beyond it
    private Property PropertyOf(string query) => Descendants(query).OfType<Property>().First();

    private InnerProperty InnerPropertyOf(string query) =>
        Descendants(query).OfType<InnerProperty>().First();

    private IEnumerable<ITreeElement> Descendants(string query)
    {
        var tree = _sut.Parse(new HLinqQuery<DummyEntity> { SourceQueryString = query },
            TestServicesCollection.Tokenizer.Tokenize(query));
        return Walk(tree);
    }

    private static IEnumerable<ITreeElement> Walk(ITreeBranch branch)
    {
        foreach (var child in branch.Children)
        {
            yield return child;

            if (child is ITreeBranch nested)
            {
                foreach (var deeper in Walk(nested))
                {
                    yield return deeper;
                }
            }
        }
    }
}
