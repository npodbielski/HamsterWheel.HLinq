using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier;

/// <summary>
/// A named aggregate call over a group, its lambda standing on a parameter of the grouping's
/// item type; the grouping's projection answers it beside <c>Key</c> and <c>Rows</c>.
/// </summary>
public sealed record GroupAggregate(string Name, LambdaExpression Selector);
