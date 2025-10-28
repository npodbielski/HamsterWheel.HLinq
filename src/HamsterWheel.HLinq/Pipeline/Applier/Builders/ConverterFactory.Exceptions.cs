using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed class MissingConverterException(Type type) : HLinqQueryException($"No converter found for type: '{type}'");