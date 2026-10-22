using FluentAssertions;
using HamsterWheel.HLinq.MariaDb;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.MariaDb;

public class MariaDbEntityFrameworkStaticMethodProviderUnitTests
{
    [Fact]
    public void Types_WhenRead_ThenIncludePomeloAndEfFunctionTranslations()
    {
        //arrange
        Type[] expected =
        [
            typeof(DbFunctionsExtensions),
            typeof(MySqlDbFunctionsExtensions),
            typeof(MySqlJsonDbFunctionsExtensions)
        ];
        var sut = new MariaDbEntityFrameworkStaticMethodProvider();

        //act
        var actual = sut.Types;

        //assert
        actual.Should().BeEquivalentTo(expected);
    }
}