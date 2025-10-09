using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

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
            throw new InvalidOperationException(
                $"Element of type {context.CurrentElement.GetType().Name} does not have any children parsers!");
        }

        var numberOfInvalidParsers = 0;
        while (context.Tokens.Length > 0)
        {
            numberOfInvalidParsers = 0;
            foreach (var parser in parsers)
            {
                if (parser.TryBuildElement(context))
                {
                    if (context.CurrentElement is TreeBranch)
                    {
                        Parse(context);
                    }

                    parser.Finish(context);
                    context.GoBackInTheTree();
                }
                else
                {
                    numberOfInvalidParsers++;
                }
            }

            if (numberOfInvalidParsers == parsers.Length) break;
        }

        if (context.CurrentElement is IHLinqQuery && numberOfInvalidParsers == parsers.Length)
        {
            throw new NonParsableTokenSequenceException(context.SourceQueryString, context.Tokens, parsers);
        }
    }
}