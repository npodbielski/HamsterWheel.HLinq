using System.Collections;
using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

public class HLinqObjectExtensionsTests
{
    [Fact]
    public void ExecuteHLinq_WhenCollectionWithWhere_ThenFiltered()
    {
        var data = Superhero.Superheroes;

        var result = data.ExecuteHLinq("where[x.Name == \"Thor\"]");

        var filtered = ((IEnumerable)result!).Cast<Superhero>().ToArray();
        filtered.Should().NotBeEmpty().And.OnlyContain(s => s.Name == "Thor");
        filtered.Should().NotBeEmpty();
    }

    [Fact]
    public void ExecuteHLinq_WhenCollectionWithSelectRenameAndOrderBy_ThenProjected()
    {
        var data = Superhero.Superheroes;

        var result = data.ExecuteHLinq("orderByDescending[x.Name].select[Who=x.Name]");

        var projected = ((IEnumerable)result!).Cast<object>().ToArray();
        projected.Should().HaveCount(data.Length);
        projected.First().GetType().GetProperty("Who").Should().NotBeNull();
    }

    [Fact]
    public void ExecuteHLinq_WhenSingleObject_ThenProjectedToFirstRow()
    {
        var hero = Superhero.Superheroes[0];

        var result = hero.ExecuteHLinq("select[Who=x.Name]");

        //single object: the projection of the one wrapped row, not a collection
        result.Should().NotBeNull();
        result!.GetType().GetProperty("Who")!.GetValue(result).Should().Be(hero.Name);
    }

    [Fact]
    public void ExecuteHLinq_WhenQueryWithoutRoot_ThenTreatedAsSelect()
    {
        var data = Superhero.Superheroes;

        var result = data.ExecuteHLinq("x.Name");

        var projected = ((IEnumerable)result!).Cast<object>().ToArray();
        projected.Should().HaveCount(data.Length);

        //single-column projection yields the column values directly
        projected.Should().ContainInOrder(data.Select(d => (object)d.Name));
    }

    [Fact]
    public void ExecuteHLinq_WhenNullObject_ThenNull()
    {
        Superhero[]? data = null;

        var result = data.ExecuteHLinq("select[name]");

        result.Should().BeNull();
    }
}
