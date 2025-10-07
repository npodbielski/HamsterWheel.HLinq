using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public sealed class HLinqParser(IHLinqParsersCollection parsersCollection) : IHLinqParser
{
    private IElementParser[] Parsers => parsersCollection.Parsers;

    private IElementParser[] RootParsers { get; } = parsersCollection.Parsers.Where(p => p.IsRoot).ToArray();

    public IHLinqQuery Parse<T>(IToken[] tokens, string stringQuery) where T : class
    {
        var query = new HLinqQuery<T>
        {
            SourceQueryString = stringQuery
        };
        var context = new ParsingContext(query)
        {
            SourceQueryString = stringQuery,
            Tokens = tokens
        };
        Parse<T>(context);
        ((ITreeBranch)query).Finish(context, context.Tokens);
        return query;
    }

    private void Parse<T>(IParsingContext context)
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
                //TODO: pass T to parser to validate if Type has properties, methods etc.
                if (parser.TryBuildElement(context))
                {
                    if (context.CurrentElement is TreeBranch)
                    {
                        Parse<T>(context);
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
            throw new NonParsableTokenSequenceException(context.Tokens, parsers);
        }
    }
}