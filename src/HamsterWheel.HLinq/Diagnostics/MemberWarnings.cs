using System.Reflection;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Reflection;

namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Hlinq entity member warning. I.e., ambiguous property name
/// </summary>
internal static class MemberWarnings
{
    /// <summary>
    /// Reports a warning
    /// </summary>
    public static void Report<T>(this MemberResolution<T> resolution, IBuilderContext? context)
        where T : MemberInfo =>
        context?.Warnings.Report(resolution, context.HLinqQuery);

    private static void Report<T>(this IHLinqWarnings? warnings, MemberResolution<T> resolution,
        string query) where T : MemberInfo
    {
        if (warnings is null
            || resolution.Reason != ResolutionReason.OrdinalTieBreak
            || resolution.Competitors.Count < 2
            || resolution.Member is null)
        {
            return;
        }

        warnings.Report(new HLinqMemberWarning(
            typeof(T) == typeof(PropertyInfo)
                ? HLinqMemberWarningReason.AmbiguousName
                : HLinqMemberWarningReason.AmbiguousCall,
            Spelled(resolution.Member),
            resolution.Member.DeclaringType?.ToString() ?? typeof(T).ToString()!,
            [.. resolution.Competitors.Select(Spelled)],
            query));
    }

    private static string Spelled<T>(T member) where T : MemberInfo =>
        member switch
        {
            MethodInfo method =>
                $"{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name))})",
            _ => member.Name,
        };
}
