using System.Linq.Expressions;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Select;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Tree.Select;

public sealed class InitializerConstantValue(IToken[] tokens) : TreeLeaf(tokens)
{
    public NameOrValue ValueToken => Tokens.OfType<NameOrValue>().First();

    public sealed class Parser : ElementParserBase<InitializerConstantValue>
    {
        protected override Type[] ValidParents { get; } = [typeof(PropertyAssignment)];

        protected override InitializerConstantValue? BuildBranch(IParsingContext context)
        {
            return context.Tokens is [Assignment, NameOrValue, ..]
                ? new InitializerConstantValue(context.Tokens[..2])
                : null;
        }
    }

    public sealed class Converter(IValueConverterFactory factory)
        : ElementToExpressionConverter<InitializerConstantValue>
    {
        protected override Expression Build(IBuilderContext context, InitializerConstantValue element)
        {
            var propertyType = context.InitializerPropertyType ??
                               throw new InitializerPropertyTypeNotInitializedException();
            var converter = factory.GetConverterFor(propertyType);
            return Expression.Constant(converter.Convert(element.ValueToken.GetValue(context.HLinqQuery)));
        }
    }
}