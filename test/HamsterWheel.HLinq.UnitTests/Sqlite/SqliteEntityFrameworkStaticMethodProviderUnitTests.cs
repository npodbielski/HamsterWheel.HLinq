using FluentAssertions;
using HamsterWheel.HLinq.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.Sqlite;

public class SqliteEntityFrameworkStaticMethodProviderUnitTests
{
    [Fact]
    public void Types_WhenRead_ThenIncludeLike()
    {
        //arrange
        Type[] expected = [typeof(DbFunctionsExtensions)];
        var sut = new SqliteEntityFrameworkStaticMethodProvider();

        //act
        var actual = sut.Types;

        //assert
        actual.Should().BeEquivalentTo(expected);
    }
}