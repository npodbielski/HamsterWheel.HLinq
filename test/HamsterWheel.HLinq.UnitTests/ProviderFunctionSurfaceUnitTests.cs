using FluentAssertions;
using HamsterWheel.HLinq.MariaDb;
using HamsterWheel.HLinq.MySql;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Sqlite;
using System.Reflection;

namespace HamsterWheel.HLinq.UnitTests;

public class ProviderFunctionSurfaceUnitTests
{
    [Fact]
    public void FunctionSurfaces_WhenMeasured_ThenMatchTheRecordedCounts()
    {
        //arrange
        IStaticMethodSource[] sources =
        [
            new MySqlEntityFrameworkStaticMethodProvider(),
            new MariaDbEntityFrameworkStaticMethodProvider(),
            new SqliteEntityFrameworkStaticMethodProvider()
        ];

        //act
        var actual = string.Join("; ", sources.Select(Describe));
        var bindable = string.Join("; ", sources.Select(DescribeBindable));

        //assert
        actual.Should().Be("MySqlEntityFrameworkStaticMethodProvider/DbFunctionsExtensions=2/1, " +
                           "MySqlEntityFrameworkStaticMethodProvider/MySqlDbFunctionsExtensions=20/2, " +
                           "MySqlEntityFrameworkStaticMethodProvider/MySqlJsonDbFunctionsExtensions=11/6; " +
                           "MariaDbEntityFrameworkStaticMethodProvider/DbFunctionsExtensions=2/1, " +
                           "MariaDbEntityFrameworkStaticMethodProvider/MySqlDbFunctionsExtensions=20/2, " +
                           "MariaDbEntityFrameworkStaticMethodProvider/MySqlJsonDbFunctionsExtensions=11/6; " +
                           "SqliteEntityFrameworkStaticMethodProvider/DbFunctionsExtensions=2/1");
        bindable.Should().Be(
            "MySqlEntityFrameworkStaticMethodProvider=JsonContainsPath,JsonSearchAny,Like; " +
            "MariaDbEntityFrameworkStaticMethodProvider=JsonContainsPath,JsonSearchAny,Like; " +
            "SqliteEntityFrameworkStaticMethodProvider=Like");
    }

    private static string Describe(IStaticMethodSource source)
    {
        return string.Join(", ", source.Types.Select(type =>
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var spellableNames = methods.Select(method => method.Name).Distinct().Count();
            var booleanNames = methods.Where(method => method.ReturnType == typeof(bool))
                .Select(method => method.Name).Distinct().Count();
            return $"{source.GetType().Name}/{type.Name}={spellableNames}/{booleanNames}";
        }));
    }

    private static string DescribeBindable(IStaticMethodSource source)
    {
        var names = source.Types.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static |
                                                                      BindingFlags.DeclaredOnly))
            .Where(method => method.ReturnType == typeof(bool))
            .Select(method => method.Name)
            .Distinct()
            .Where(name => CanBind(source, name))
            .Order()
            .ToArray();
        return $"{source.GetType().Name}={string.Join(',', names)}";
    }

    private static bool CanBind(IStaticMethodSource source, string name)
    {
        try
        {
            ProviderFunctionsUnitTests.Filter(source, $"where[{name}(x.Name, abc)]");
            return true;
        }
        catch
        {
            return false;
        }
    }
}