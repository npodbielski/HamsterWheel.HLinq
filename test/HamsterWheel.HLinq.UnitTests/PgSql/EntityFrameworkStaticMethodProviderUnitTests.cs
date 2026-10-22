using FluentAssertions;
using HamsterWheel.HLinq.PgSql;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.PgSql;

public class EntityFrameworkStaticMethodProviderUnitTests
{
    [Fact]
    public void Types_WhenRead_ThenAreTheFunctionsAFilterCanAnswerWith()
    {
        //arrange
        Type[] expected = [typeof(NpgsqlDbFunctionsExtensions), typeof(NpgsqlTrigramsDbFunctionsExtensions)];
        var sut = new PgSqlEntityFrameworkStaticMethodProvider();

        //act
        var actual = sut.Types;

        //assert
        actual.Should().BeEquivalentTo(expected);
    }
}