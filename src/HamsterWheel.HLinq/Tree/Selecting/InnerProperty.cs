using System.Linq.Expressions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// Nested property in nested select. I.e.: <c>a.City</c> in <c>x.Addresses.select(a.City)</c>.
/// </summary>
public sealed class InnerProperty(IToken[] tokens) : TreeLeaf(tokens)
{
    public string[] GetPath(string hLinqQuery) =>
        Tokens.Any(t => t is PropertyName)
            ? [.. Tokens.OfType<PropertyName>().Select(t => t.GetValue(hLinqQuery))]
            : [Tokens.OfType<NameOrValue>().First().GetValue(hLinqQuery)];

    public string GetLastName(string hLinqQuery) => GetPath(hLinqQuery).Last();

    // ReSharper disable once UnusedType.Global - Used by Parser
    public sealed class Parser : ElementParserBase<InnerProperty>
    {
        protected override Type[] ValidParents { get; } = [typeof(PropertyAssignment)];
        public override IToken[] ExampleTokens { get; } =
            [SubEntity.Empty, Dot.Empty, PropertyName.Empty];

        protected override InnerProperty? BuildBranch(IParsingContext context)
        {
            var index = 0;

            //the rename's `=` is left to this path the same way `Property` takes it: the name on
            //its left is already an InitializerPropertyName of its own
            if (context.Tokens[0] is Assignment)
            {
                index += 1;
            }

            if (context.Tokens[index..] is [SubEntity, Dot, PropertyName, .. var rest])
            {
                index += 3;

                while (rest is [Dot, PropertyName, .. var next])
                {
                    rest = next;
                    index += 2;
                }

                return new InnerProperty(context.Tokens[..index]);
            }

            //a bare name is the element's own member only where an element is in scope - inside
            //a nested select, one level above this assignment - and only when it is not the
            //left side of a rename, which InitializerPropertyName reads together with its `=`
            if (index == 0 && context.Tokens[index..] is [NameOrValue name, Comma or RightCircleBracket, ..] &&
                context.CurrentBranch is PropertyAssignment && context.Parents.Peek().Element is NestedSelect)
            {
                return new InnerProperty([name]);
            }

            return null;
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Converter : ElementToExpressionConverter<InnerProperty>
    {
        protected override Expression Build(IBuilderContext context, InnerProperty element)
        {
            var scope = context as INestedScopeBuilderContext ??
                        throw new InnerPropertyOutsideOfNestedSelectException();
            var path = element.GetPath(context.HLinqQuery);
            return context.Builder.GetProperty(scope.InnerType, scope.InnerSource, path, context).Member;
        }

        public sealed class InnerPropertyOutsideOfNestedSelectException()
            : HLinqQueryException(
                $"{nameof(InnerProperty)} can only be translated inside a nested select, where the element it walks has a scope.");
    }
}
