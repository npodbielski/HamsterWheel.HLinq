using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

/// <summary>
/// A clause that filters or orders, answering what every such clause shares.
/// </summary>
/// <typeparam name="T">The root the parser builds.</typeparam>
public abstract class OrderingOrFilteringRootParser<T> : ElementParserBase<T> where T : ITreeElement
{
    protected override void FinishImpl(IParsingContext context)
    {
        if (context.Tokens is not [RightSquareBracket bracket, ..])
        {
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                [RightSquareBracket.Empty]);
        }

        context.CurrentBranch?.Finish(context, [bracket]);
        context.CheckAfterGroup();

        context.RemoveStartTokens(1);
    }
}
