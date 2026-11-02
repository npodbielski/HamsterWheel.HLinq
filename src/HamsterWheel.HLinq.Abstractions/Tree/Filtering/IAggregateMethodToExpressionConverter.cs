using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

/// <summary>
/// Translates an aggregate method call of a query to an <see cref="Enumerable"/> call.
/// </summary>
public interface IAggregateMethodToExpressionConverter
{
    /// <summary>
    /// The call written on the entity itself (<c>x.Count()</c>, <c>x.Max(x.Age)</c>): bound over
    /// the parameter of <paramref name="context" />, which after a grouping stands on the group.
    /// </summary>
    Expression BuildOverEntity(IBuilderContext context, IMethod method);

    /// <summary>
    /// The aggregate call over a collection the query reached through a property, null when the
    /// name folds no <see cref="Enumerable"/> aggregate over <paramref name="source" /> - an
    /// ordinary method of a member is a different, unwritten thing, not a failed aggregate.
    /// </summary>
    Expression? BindOverMember(IBuilderContext context, IMethod method, Expression source);

    /// <summary>
    /// The type the aggregate call answers, without building it.
    /// </summary>
    Type ReturnType(IBuilderContext context, IMethod method, Type sourceType);
}
