using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Builders;

public sealed class MissingConverterException(Type type) : HLinqQueryException($"No converter found for type: '{type}'");