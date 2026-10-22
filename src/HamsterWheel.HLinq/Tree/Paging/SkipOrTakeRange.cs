using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tree.Paging;

/// <summary>
/// Leaf-like branch of the <c>take[start..end]</c> range paging syntax. Wraps
/// <c>RangeOperator [SkipOrTakeConstant]</c> with either side optional
/// (<c>take[10..]</c>, <c>take[..20]</c>). The start number is a part of the
/// <see cref="RangeOperator" /> token itself (<c>10..</c>) - that is how the tokenizer tells
/// <c>take[10..20]</c> from <c>take[10.2]</c> without backtracking. Valid only under
/// <see cref="TakeRoot" /> - <c>skip</c> keeps its single value.
/// </summary>
public sealed class SkipOrTakeRange(IToken[] tokens) : TreeBranch(tokens)
{
    /// <summary>
    /// End of the range (exclusive rows count) or <c>null</c> for open end (<c>take[start..]</c>).
    /// </summary>
    public SkipOrTakeConstant? EndConstant => GetChildOfType<SkipOrTakeConstant>();

    /// <summary>
    /// Start of the range (skipped rows count) as written, or <c>null</c> for open start
    /// (<c>take[..end]</c>). Read from the <see cref="RangeOperator" /> token.
    /// </summary>
    public string? GetStart(string query) => Tokens.OfType<RangeOperator>().Single().GetStartRaw(query);

    public string? GetEnd(string query) => EndConstant?.Value.GetValue(query);

    public sealed class Parser : ElementParserBase<SkipOrTakeRange>
    {
        public override IToken[] ExampleTokens { get; } =
            [new TokenExample("10.."), new TokenExample("20")];

        protected override Type[] ValidParents { get; } = [typeof(TakeRoot)];

        protected override SkipOrTakeRange? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [RangeOperator, ..] => new SkipOrTakeRange(context.Tokens[..1]),
                _ => null
            };

        protected override void FinishImpl(IParsingContext context)
        {
            // context.Children not TreeBranch.Children - those are filled by Finish() below
            if (context.CurrentElement is SkipOrTakeRange range &&
                range.GetStart(context.SourceQueryString) is null &&
                context.Children.Count == 0)
            {
                // take[..] expresses nothing
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    TakeRoot.Parser.TakeRootExampleTokens);
            }

            base.FinishImpl(context);
        }
    }
}
