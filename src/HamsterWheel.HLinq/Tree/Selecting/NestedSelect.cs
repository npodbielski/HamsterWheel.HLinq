using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// Nested select: <c>x.Addresses.select(...)</c> in
/// <c>persons?select[x.Addresses.select(a.City,a.Street)]</c>.
/// </summary>
public sealed class NestedSelect(IToken[] tokens) : TreeBranch(tokens)
{
    public IReadOnlyList<PropertyAssignment> Members => field ??= [.. Children.OfType<PropertyAssignment>()];

    // ReSharper disable once UnusedType.Global - Used by Parser
    public sealed class Parser(IGrammar grammar) : ElementParserBase<NestedSelect>
    {
        /// <summary>
        /// The call is spelled by the same word as the clause root, so a language that names the
        /// clause names the call with it - the tree it opens is this one under every spelling.
        /// </summary>
        private readonly string[] _selectWords = grammar.KeywordsOf(typeof(Select));
        protected override Type[] ValidParents { get; } = [typeof(PropertyAssignment)];

        public override IToken[] ExampleTokens { get; } =
        [
            Dot.Empty, MethodName.Empty, LeftCircleBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightCircleBracket.Empty
        ];

        protected override NestedSelect? BuildBranch(IParsingContext context) =>
            context.Tokens is [Dot, MethodName name, LeftCircleBracket, ..] && IsSelectWord(name, context)
                ? new NestedSelect(context.Tokens[..3])
                : null;

        /// <summary>
        /// Only the select word opens a nested select; another call hanging on the same property
        /// (`x.Addresses.Count()`) is declined here so the call parser can claim the tokens.
        /// </summary>
        private bool IsSelectWord(MethodName name, IParsingContext context) =>
            _selectWords.Any(w => name
                .GetValue(context.SourceQueryString)
                .Equals(w, StringComparison.OrdinalIgnoreCase));

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightCircleBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [RightCircleBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }
    }
}
