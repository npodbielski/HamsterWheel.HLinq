using System.Linq.Expressions;
using System.Reflection;
using static HamsterWheel.HLinq.Reflection.ResolutionReason;

namespace HamsterWheel.HLinq.Reflection;

/// <summary>
/// Ranks type members how close they are to the user query.
/// </summary>
internal static class MemberRanking
{
    private const int Exact = 0;
    private const int ImplicitConversion = 1;
    private const int ParsedFromQueryText = 2;
    private const int DeclaredOnType = 0;
    private const int Inherited = 1;
    private const int OfObject = 2;
    private const int NotBindableOrder = int.MaxValue;

    public static MemberResolution<PropertyInfo> RankProperties(Type type, string name,
        IReadOnlyList<PropertyInfo> propertiesOfTheType)
    {
        var named = new List<PropertyInfo>();
        foreach (var property in propertiesOfTheType)
        {
            if (property.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                named.Add(property);
            }
        }

        return Rank(type, name, named, null);
    }

    public static MemberResolution<MethodInfo> RankMethods(Type? declaredOnType, string? name,
        IReadOnlyList<MethodInfo> candidates,
        IReadOnlyList<(string value, MemberExpression? expression)>? arguments,
        Func<ParameterInfo[], ParameterInfo[]>? parametersTransformer = null) =>
        Rank(declaredOnType, name, candidates,
            method => Cost(ParametersOf(method, parametersTransformer), arguments));

    private static ParameterInfo[] ParametersOf(MethodInfo method,
        Func<ParameterInfo[], ParameterInfo[]>? parametersTransformer) =>
        parametersTransformer is null
            ? method.GetParameters()
            : parametersTransformer(method.GetParameters());

    private static MemberResolution<T> Rank<T>(Type? declaredOnType, string? name, IReadOnlyList<T> candidates,
        Func<T, int?>? conversionCost) where T : MemberInfo
    {
        if (candidates.Count == 0)
        {
            return new MemberResolution<T>(null, [], NoCandidate);
        }

        var ranked = new Ranked<T>[candidates.Count];
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            ranked[i] = new Ranked<T>(candidate, conversionCost is null ? Exact : (int?)conversionCost(candidate),
                SpelledAsWritten(name, candidate), DeclaringTier(declaredOnType, candidate));
        }

        Order(ranked);

        var competitors = new T[ranked.Length];
        for (var i = 0; i < ranked.Length; i++)
        {
            competitors[i] = ranked[i].Member;
        }

        var winner = FirstBindable(ranked, 0);
        if (winner < 0)
        {
            return new MemberResolution<T>(null, competitors, NoBindableCandidate);
        }

        var runnerUp = FirstBindable(ranked, winner + 1);

        return new MemberResolution<T>(ranked[winner].Member, competitors,
            runnerUp < 0
                ? candidates.Count == 1 ? ResolutionReason.OnlyCandidate : ResolutionReason.OnlyBindableCandidate
                : DecidedBy(ranked[winner], ranked[runnerUp]));
    }

    private static int FirstBindable<T>(Ranked<T>[] ranked, int from) where T : MemberInfo
    {
        for (var i = from; i < ranked.Length; i++)
        {
            if (ranked[i].Bindable)
            {
                return i;
            }
        }

        return -1;
    }

    private static void Order<T>(Ranked<T>[] ranked) where T : MemberInfo
    {
        for (var i = 1; i < ranked.Length; i++)
        {
            var item = ranked[i];
            var j = i - 1;

            while (j >= 0 && Compare(ranked[j], item) > 0)
            {
                ranked[j + 1] = ranked[j];
                j--;
            }

            ranked[j + 1] = item;
        }
    }

    private static int Compare<T>(Ranked<T> left, Ranked<T> right) where T : MemberInfo
    {
        var byConversion = ConversionOrder(left).CompareTo(ConversionOrder(right));
        if (byConversion != 0)
        {
            return byConversion;
        }

        var bySpelling = left.Spelling.CompareTo(right.Spelling);
        if (bySpelling != 0)
        {
            return bySpelling;
        }

        var byDeclaration = left.DeclaringTier.CompareTo(right.DeclaringTier);

        return byDeclaration != 0 ? byDeclaration : CompareOrdinal(left.Member, right.Member);
    }

    private static int ConversionOrder<T>(Ranked<T> ranked) where T : MemberInfo =>
        ranked.Bindable ? ranked.ConversionCost : NotBindableOrder;

    private static int CompareOrdinal(MemberInfo left, MemberInfo right)
    {
        var byName = string.CompareOrdinal(left.Name, right.Name);
        if (byName != 0)
        {
            return byName;
        }

        var bySignature = string.CompareOrdinal(SignatureOf(left), SignatureOf(right));
        if (bySignature != 0)
        {
            return bySignature;
        }

        return string.CompareOrdinal(left.DeclaringType?.Name, right.DeclaringType?.Name);
    }

    private static string SignatureOf(MemberInfo member) =>
        member switch
        {
            MethodInfo method => string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name)),
            PropertyInfo property => property.PropertyType.Name,
            _ => string.Empty
        };

    private static ResolutionReason DecidedBy<T>(Ranked<T> winner, Ranked<T> runnerUp) where T : MemberInfo
    {
        if (winner.ConversionCost != runnerUp.ConversionCost)
        {
            return CheapestConversion;
        }

        if (winner.Spelling != runnerUp.Spelling)
        {
            return SpelledExactlyAsWritten;
        }

        return winner.DeclaringTier != runnerUp.DeclaringTier
            ? ResolutionReason.DeclaredOnType
            : ResolutionReason.OrdinalTieBreak;
    }

    private static int? Cost(ParameterInfo[] parameters,
        IReadOnlyList<(string value, MemberExpression? expression)>? arguments)
    {
        if (arguments is null)
        {
            return Exact;
        }

        if (parameters.Length != arguments.Count)
        {
            return null;
        }

        var cost = 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (CostOf(parameters[i], arguments[i]) is not { } parameterCost)
            {
                return null;
            }

            cost += parameterCost;
        }

        return cost;
    }

    private static int? CostOf(ParameterInfo parameter, (string value, MemberExpression? expression) argument) =>
        argument.expression is not null
            ? CostOfSource(argument.expression.Type, parameter.ParameterType)
            : CostOfCast(argument.value, parameter.ParameterType);

    private static int? CostOfSource(Type sourceType, Type parameterType)
    {
        //checks for argument type
        if (!parameterType.AcceptsArgumentOfType(sourceType))
        {
            return null;
        }

        return sourceType == parameterType ? Exact : ImplicitConversion;
    }

    /// <summary>
    /// Roughly estimates how costly it is to cast part of a query, extracted from value token to destination type (parameter or property)
    /// </summary>
    private static int? CostOfCast(string value, Type parameterType)
    {
        if (parameterType == typeof(string))
        {
            return Exact;
        }

        if (parameterType == typeof(char))
        {
            return value.Length == 1 ? ParsedFromQueryText : null;
        }

        return parameterType.IsEnum && NamesAMemberOf(value, parameterType) ? ParsedFromQueryText : null;
    }

    private static bool NamesAMemberOf(string value, Type enumType)
    {
        if (value.IndexOf('.') < 0)
        {
            return value.TryParseEnum(enumType) is (true, _);
        }

        var splits = value.Split('.');

        return string.Equals(splits[0], enumType.Name, StringComparison.CurrentCultureIgnoreCase)
               && splits[1].TryParseEnum(enumType) is (true, _);
    }

    private static int SpelledAsWritten(string? name, MemberInfo member) =>
        name is null || member.Name.Equals(name, StringComparison.Ordinal) ? 0 : 1;

    private static int DeclaringTier(Type? declaredOnType, MemberInfo member)
    {
        var declaringType = member.DeclaringType;

        if (declaringType is null || declaredOnType is null)
        {
            return Inherited;
        }

        if (declaringType == declaredOnType)
        {
            return DeclaredOnType;
        }

        return declaringType == typeof(object) ? OfObject : Inherited;
    }

    private readonly struct Ranked<T>(T member, int? conversionCost, int spelling, int declaringTier)
        where T : MemberInfo
    {
        public T Member { get; } = member;

        public int ConversionCost { get; } = conversionCost ?? Exact;

        public bool Bindable { get; } = conversionCost.HasValue;

        public int Spelling { get; } = spelling;

        public int DeclaringTier { get; } = declaringTier;
    }
}