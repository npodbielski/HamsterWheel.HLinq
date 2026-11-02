using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.TestUtils;

/// <summary>
/// Asks every declared name of every class a pack hands over the question its own signature asks, and
/// sorts the names into the three sets of the surface: what answers with a truth, what answers with a
/// value a comparison of the row can be made of, and what stays out of reach with the reason.
/// </summary>
/// <remarks>
/// A parameter is filled with the first column of <see cref="DummyEntity"/> or written value that
/// <see cref="Expression.Call(Type, MethodInfo, IEnumerable{Expression})"/> accepts for it, so the
/// framework's own type check decides acceptance and the probe restates no rule of the binder. The
/// binder is then asked the question a query asks: whether the call builds a filter through
/// <see cref="ExpressionBuilder.GetFilter"/>.
/// </remarks>
public sealed class FunctionSurfaceProbe(IStaticMethodSource source)
{
    private static readonly TestServicesCollection Services = new();
    private static readonly IHLinqParser Parser = new HLinqParser(Services.Parsers);

    private static readonly ParameterExpression Row = Expression.Parameter(typeof(DummyEntity), "x");

    private static readonly (string QueryText, MemberExpression Expression)[] Columns = BuildColumns();

    public FunctionSurface Measure()
    {
        var counts = source.Types.Select(DescribeClass).ToArray();

        var filters = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var reasons = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in source.Types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static |
                                                   BindingFlags.DeclaredOnly))
            {
                var outcome = ProbeMethod(method);
                switch (outcome.Set)
                {
                    case NameSet.Filter:
                        filters.Add(method.Name);
                        break;
                    case NameSet.Value:
                        values.Add(method.Name);
                        break;
                    default:
                        AddReason(reasons, method.Name, outcome.Reason);
                        break;
                }
            }
        }

        //a name is in a set when any overload of it lands there, which is how the binder sees
        //competitors; only a name no overload lands in a reachable set is out of reach
        var outOfReach = new List<string>();
        foreach (var (name, reasonsOfName) in reasons)
        {
            if (!filters.Contains(name) && !values.Contains(name))
            {
                outOfReach.Add($"{name}: {string.Join("; ", reasonsOfName)}");
            }
        }

        return new FunctionSurface(
            source.GetType().Name,
            [.. counts],
            [.. filters],
            [.. values],
            [.. outOfReach]);
    }

    private string DescribeClass(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var spellableNames = methods.Select(method => method.Name).Distinct().Count();
        var booleanNames = methods.Where(method => method.ReturnType == typeof(bool))
            .Select(method => method.Name).Distinct().Count();
        return $"{source.GetType().Name}/{type.Name}={spellableNames}/{booleanNames}";
    }

    private static (string QueryText, MemberExpression Expression)[] BuildColumns()
    {
        var columns = new List<(string, MemberExpression)>();
        foreach (var property in typeof(DummyEntity).GetProperties())
        {
            if (property.PropertyType == typeof(NestedDummyEntity))
            {
                var nested = typeof(NestedDummyEntity).GetProperty("Name")!;
                columns.Add(($"x.{property.Name}.{nested.Name}",
                    Expression.Property(Expression.Property(Row, property), nested)));
                continue;
            }

            columns.Add(($"x.{property.Name}", Expression.Property(Row, property)));
        }

        return [.. columns];
    }

    private Outcome ProbeMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();

        if (method.IsGenericMethodDefinition)
        {
            return Outcome.OutOfReach("is generic in the type of its argument, which no query names");
        }

        var start = 0;
        Expression? thisParameter = null;
        if (parameters.Length > 0 && EfDbFunctionsMatcher.IsEfDbFunctionParameter(parameters[0]))
        {
            thisParameter = Expression.Constant(null, parameters[0].ParameterType);
            start = 1;
        }

        var queryArguments = new (string QueryText, Expression Expression)[parameters.Length - start];
        for (var i = start; i < parameters.Length; i++)
        {
            var filling = FirstFillingThatBinds(method, parameters, i);
            if (filling is null)
            {
                return Outcome.OutOfReach(
                    $"parameter {parameters[i].ParameterType.Name} {parameters[i].Name} binds no column and no written value");
            }

            queryArguments[i - start] = filling.Value;
        }

        if (queryArguments.Length == 0)
        {
            return Outcome.OutOfReach("no argument between the parentheses");
        }

        var callArguments = new List<Expression>();
        if (thisParameter is not null)
        {
            callArguments.Add(thisParameter);
        }

        callArguments.AddRange(queryArguments.Select(a => a.Expression));
        var call = Expression.Call(null, method, callArguments);

        var arguments = string.Join(", ", queryArguments.Select(a => a.QueryText));
        var callText = $"{method.Name}({arguments})";

        if (method.ReturnType == typeof(bool))
        {
            return BuildsFilter($"where[{callText}]")
                ? Outcome.AsFilter()
                : Outcome.OutOfReach($"binder refused where[{callText}]");
        }

        foreach (var comparison in ComparisonsOf(call.Type, callText))
        {
            if (BuildsFilter(comparison))
            {
                return Outcome.AsValue();
            }
        }

        return Outcome.OutOfReach($"returns {method.ReturnType.Name}, which nothing of a query compares with");
    }

    private static (string QueryText, Expression Expression)? FirstFillingThatBinds(MethodInfo method,
        ParameterInfo[] parameters, int index)
    {
        var parameterType = parameters[index].ParameterType;

        foreach (var column in Columns)
        {
            if (Accepts(method, parameters, index, column.Expression))
            {
                return column;
            }
        }

        if (parameterType == typeof(string))
        {
            return ("abc", Expression.Constant("abc"));
        }

        if (parameterType == typeof(char))
        {
            return ("a", Expression.Constant('a'));
        }

        if (parameterType.IsEnum)
        {
            var member = Enum.GetValues(parameterType).GetValue(0)!;
            return (Enum.GetName(parameterType, member)!, Expression.Constant(member));
        }

        var written = WrittenValueOf(parameterType);
        if (written is not null)
        {
            return written;
        }

        return null;
    }

    /// <summary>
    /// A value a query writes for a parameter of the type, in one of the shapes the parse table
    /// reads - the same fillings a query may hand a function since a written argument reaches a
    /// parameter of any type the table parses.
    /// </summary>
    private static (string QueryText, Expression Expression)? WrittenValueOf(Type parameterType)
    {
        var unwrapped = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

        object? value = unwrapped switch
        {
            _ when unwrapped == typeof(byte) => (byte)7,
            _ when unwrapped == typeof(short) => (short)7,
            _ when unwrapped == typeof(int) => 7,
            _ when unwrapped == typeof(long) => 7L,
            _ when unwrapped == typeof(decimal) => 7.5m,
            _ when unwrapped == typeof(double) => 7.5d,
            _ when unwrapped == typeof(float) => 7.5f,
            _ when unwrapped == typeof(Guid) => Guid.Empty,
            _ when unwrapped == typeof(DateTime) => new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            _ when unwrapped == typeof(DateTimeOffset) => new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            _ when unwrapped == typeof(TimeSpan) => TimeSpan.FromMinutes(2),
#if !NETSTANDARD
            _ when unwrapped == typeof(DateOnly) => new DateOnly(2020, 1, 1),
            _ when unwrapped == typeof(TimeOnly) => new TimeOnly(14, 30),
#endif
            _ => null
        };

        if (value is null)
        {
            return null;
        }

        var text = unwrapped switch
        {
            _ when unwrapped == typeof(Guid) => "110b4b9b-0a0a-4a0a-9a0a-0a0a0a0a0a0a",
            _ when unwrapped == typeof(DateTime) || unwrapped == typeof(DateTimeOffset) => "2020-01-01T14:30",
            _ when unwrapped == typeof(DateOnly) => "2020-01-01",
            _ when unwrapped == typeof(TimeSpan) || unwrapped == typeof(TimeOnly) => "14:30",
            _ when unwrapped == typeof(decimal) || unwrapped == typeof(double) || unwrapped == typeof(float) =>
                "7.5",
            _ => "7"
        };

        return (text, Expression.Constant(value, parameterType));
    }

    private static bool Accepts(MethodInfo method, ParameterInfo[] parameters, int index, Expression candidate)
    {
        var arguments = new Expression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = i == index
                ? candidate
                : DefaultConstant(parameters[i].ParameterType);
        }

        try
        {
            Expression.Call(null, method, (IEnumerable<Expression>)arguments);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static ConstantExpression DefaultConstant(Type type) =>
        Expression.Constant(type.IsValueType ? Activator.CreateInstance(type) : null, type);

    private static IEnumerable<string> ComparisonsOf(Type returnType, string callText)
    {
        //a column of the return type is the comparison a row makes of the value; a written value is
        //the one a query makes of it
        foreach (var column in Columns)
        {
            if (column.Expression.Type == returnType)
            {
                yield return $"where[{callText}=={column.QueryText}]";
            }
        }

        var written = WrittenComparisonOf(returnType);
        if (written is not null)
        {
            yield return $"where[{callText}{written.Value.Operator} {written.Value.Value}]";
        }
    }

    private static (string Operator, string Value)? WrittenComparisonOf(Type returnType)
    {
        var unwrapped = Nullable.GetUnderlyingType(returnType) ?? returnType;

        if (unwrapped == typeof(string))
        {
            return ("==", "abc");
        }

        if (unwrapped == typeof(char))
        {
            return ("==", "a");
        }

        if (unwrapped == typeof(int) || unwrapped == typeof(long) || unwrapped == typeof(short) ||
            unwrapped == typeof(byte) || unwrapped == typeof(decimal) || unwrapped == typeof(double) ||
            unwrapped == typeof(float))
        {
            return (">", "7");
        }

        if (unwrapped == typeof(TimeSpan))
        {
            return (">", "00:02:00");
        }

#if !NETSTANDARD
        if (unwrapped == typeof(DateOnly))
        {
            return ("==", "2020-01-01");
        }
#endif

        if (unwrapped.IsEnum)
        {
            return ("==", Enum.GetName(unwrapped, Enum.GetValues(unwrapped).GetValue(0)!)!);
        }

        return null;
    }

    private bool BuildsFilter(string query)
    {
        try
        {
            var builder = new ExpressionBuilder(Services.PropertiesCache, Services.MethodsCache,
                Services.ConverterFactory, new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
                [source], Services.WrittenValues, null!);
            var tokens = Services.Tokenizer.Tokenize(query);
            var tree = Parser.TestParseEntryPoint<DummyEntity>(query, tokens);
            builder.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query,
                warnings: new HLinqWarnings());
            return true;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return false;
        }
    }

    private enum NameSet
    {
        Filter,
        Value,
        OutOfReach
    }

    private readonly record struct Outcome(NameSet Set, string? Reason)
    {
        public static Outcome AsFilter() => new(NameSet.Filter, null);

        public static Outcome AsValue() => new(NameSet.Value, null);

        public static Outcome OutOfReach(string reason) => new(NameSet.OutOfReach, reason);
    }

    private static void AddReason(SortedDictionary<string, List<string>> reasons, string name, string? reason)
    {
        if (reason is null)
        {
            return;
        }

        if (!reasons.TryGetValue(name, out var list))
        {
            list = [];
            reasons[name] = list;
        }

        if (!list.Contains(reason))
        {
            list.Add(reason);
        }
    }
}

public sealed record FunctionSurface(
    string SourceName,
    string[] ClassCounts,
    string[] FilterNames,
    string[] ValueNames,
    string[] OutOfReachNames);
