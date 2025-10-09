using System.Linq.Expressions;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Ordering;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Tree;

public sealed class Property(IToken[] tokens) : TreeLeaf(tokens), IMethodParamElement
{
    public string GetValue(string hLinqQuery) => string.Join('.', GetPath(hLinqQuery));

    public string[] GetPath(string hLinqQuery) =>
        Tokens.OfType<PropertyAccess>().Select(t => t.GetValue(hLinqQuery)).ToArray();

    public sealed class Parser : ElementParserBase<Property>
    {
        public override IToken[] ExampleTokens { get; } =
            [new Entity(default), new Dot(default), new PropertyAccess(default)];

        protected override Type[] ValidParents { get; } =
        [
            typeof(Condition), typeof(Method), typeof(OrderByRoot), typeof(OrderByDescendingRoot), typeof(ThenByRoot),
            typeof(ThenByDescendingRoot), typeof(PropertyAssignment)
        ];

        protected override Property? BuildBranch(IParsingContext context)
        {
            var index = 0;

            if (context.Tokens.First() is Assignment)
            {
                index += 1;
            }

            if (context.Tokens[index..] is not [Entity, Dot, PropertyAccess, .. var r1])
            {
                return null;
            }

            var rest = r1;

            index += 3;

            while (rest is [Dot, PropertyAccess, .. var r2])
            {
                rest = r2;
                index += 2;
            }

            return new Property(context.Tokens[..index]);
        }
    }

    public sealed class Converter : ElementToExpressionConverter<Property>
    {
        protected override Expression Build(IBuilderContext context, Property element)
        {
            var path = element.GetPath(context.HLinqQuery);
            return context.Builder.GetProperty(context.Type, context.Param, path).Member;
        }
    }
}