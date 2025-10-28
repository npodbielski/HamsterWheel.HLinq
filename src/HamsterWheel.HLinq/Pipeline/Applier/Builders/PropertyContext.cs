using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed record PropertyContext(MemberExpression Member, Type Type) : IPropertyContext;