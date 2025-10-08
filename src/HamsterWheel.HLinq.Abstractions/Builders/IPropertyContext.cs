using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Builders;

public interface IPropertyContext
{
    MemberExpression Member { get; init; }
}