using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Builders;

public sealed record PropertyContext(MemberExpression Member, Type Type) : IPropertyContext;