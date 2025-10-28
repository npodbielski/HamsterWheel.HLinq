using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IPropertyContext
{
    MemberExpression Member { get; init; }
}