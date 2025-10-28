using System.Linq.Expressions;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Selecting;

public sealed class InitializerConstantValue(IToken[] tokens) : TreeLeaf(tokens)
{
    public NameOrValue ValueToken => Tokens.OfType<NameOrValue>().First();

    public sealed class Parser : ElementParserBase<InitializerConstantValue>
    {
        public override IToken[] ExampleTokens { get; } = [NameOrValue.Empty];
        protected override Type[] ValidParents { get; } = [typeof(PropertyAssignment)];

        protected override InitializerConstantValue? BuildBranch(IParsingContext context) =>
            context.Tokens is [Assignment, NameOrValue, ..]
                ? new InitializerConstantValue(context.Tokens[..2])
                : null;
    }

    public sealed class Converter(IDefaultConverter defaultConverter)
        : ElementToExpressionConverter<InitializerConstantValue>
    {
        protected override Expression Build(IBuilderContext context, InitializerConstantValue element)
        {
            var propertyType = (context as InitializerPropertyAssignmentBuilderContext)?.InitializerPropertyType ??
                               throw new InitializerPropertyTypeNotInitializedException();
            var value = element.ValueToken.GetValue(context.HLinqQuery);
            return Expression.Constant(defaultConverter.ConvertTo(propertyType, value));
        }

        private class InitializerPropertyTypeNotInitializedException()
            : HLinqQueryException(
                $"When building expression for {nameof(InitializerConstantValue)} property type of final object must be known. Make sure that expression is built from inside {nameof(PropertyAssignment)}");
    }
}