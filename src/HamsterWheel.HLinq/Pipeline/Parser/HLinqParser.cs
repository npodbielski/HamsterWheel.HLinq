using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public sealed class HLinqParser(IEnumerable<IElementParser> allParsers) : IHLinqParser
{
    private IElementParser[] Parsers => allParsers.ToArray();

    private IElementParser[] RootParsers { get; } = allParsers.Where(p => p.IsRoot).ToArray();

    public IHLinqQuery Parse(IHLinqQuery query, IToken[] tokens)
    {
        var context = new ParsingContext(query)
        {
            Tokens = tokens
        };
        Parse(context);
        query.Finish(context, context.Tokens);
        return query;
    }

    private void Parse(IParsingContext context)
    {
        var parsers = context.CurrentElement is IHLinqQuery
            ? RootParsers
            : Parsers.Where(p => p.ChildOf(context.CurrentElement)).ToArray();
        if (parsers.Length == 0 && !context.CurrentElement.NoChildren)
        {
            throw new MissingParserException(context);
        }

        var numberOfInvalidParsers = 0;
        while (context.Tokens.Length > 0)
        {
            numberOfInvalidParsers = 0;
            var tokensBefore = context.Tokens.Length;
            var depthBefore = context.Parents.Count;
            foreach (var parser in parsers)
            {
                if (parser.TryBuildElement(context))
                {
                    if (context.CurrentElement is TreeBranch)
                    {
                        Parse(context);
                    }

                    parser.Finish(context);
                    context.GoToParent();
                }
                else
                {
                    numberOfInvalidParsers++;
                }
            }

            if (numberOfInvalidParsers == parsers.Length) break;

            // A parser may build an element without consuming a token (Condition is
            // a frame its children parse into). A pass that built at least one
            // element yet consumed no token and ended at the same tree depth made
            // no progress; the parse is deterministic, so repeating it would loop
            // forever (e.g. `where[x` - Condition matches [Entity, ..] and nothing
            // consumes the dangling Entity).
            if (context.Tokens.Length == tokensBefore && context.Parents.Count == depthBefore)
            {
                throw new NonParsableTokenSequenceException(context.SourceQueryString, context.Tokens, parsers);
            }
        }

        if (context.CurrentElement is IHLinqQuery && numberOfInvalidParsers >= parsers.Length)
        {
            throw new NonParsableTokenSequenceException(context.SourceQueryString, context.Tokens, parsers);
        }
    }

    private sealed class MissingParserException(IParsingContext context)
        : HLinqQueryException(
            $"Element of type {context.CurrentElement.GetType().Name} does not have any children parsers!");
}