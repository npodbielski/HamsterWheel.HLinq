using FluentAssertions;
using HamsterWheel.HLinq.MySql;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.MySql;

public class MySqlEntityFrameworkStaticMethodProviderUnitTests
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
        var sut = new MySqlEntityFrameworkStaticMethodProvider();

        //act
        var actual = sut.Types;

        //assert
        actual.Should().BeEquivalentTo(expected);
    }
}