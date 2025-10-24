namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class MethodCall : MemberAccess
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<MethodCall>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static MethodCall Build(Range range) => new() { Range = range };
    public static MethodCall Empty { get; } = new() { Range = default };
}