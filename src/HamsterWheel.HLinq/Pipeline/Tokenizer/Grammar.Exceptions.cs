using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public sealed class GrammarValidationException(IReadOnlyList<string> problems)
    : HLinqQueryException(
        $"HLinq grammar is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems.Select(p => $" - {p}"))}");
