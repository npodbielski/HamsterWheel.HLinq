using System.Linq.Expressions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed partial class Method(IToken[] tokens) : TreeBranch(tokens), IMethod
{
    private MethodSource? _source;
    private MethodName? _method;

    private MethodSource SourceType => _source ?? Tokens.First() switch
    {
        Dot => _source ??= MethodSource.Property,
        NameOrValue => _source ??= MethodSource.Constant,
        _ => _source ??= MethodSource.Static
    };

    private MethodName MethodName => _method ??= Tokens.OfType<MethodName>().First();

    public string GetName(string hLinqQuery) => MethodName.GetValue(hLinqQuery);

    public sealed class Parser : ElementParserBase<Method>
    {
        protected override Type[] ValidParents { get; } = [typeof(Condition)];

        public override IToken[] ExampleTokens { get; } =
            [MethodName.Empty, LeftCircleBracket.Empty, RightCircleBracket.Empty];

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightCircleBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens.Take(5).ToArray(),
                    [RightCircleBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }

        protected override Method? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Dot, MethodName _, LeftCircleBracket, ..] => new Method(context.Tokens[..3]),
                [MethodName _, LeftCircleBracket, ..] => new Method(context.Tokens[..2]),
                _ => null
            };
    }

    public sealed class Converter(
        IPropertyMethodToExpressionConverter propertyMethodConverter,
        IStaticMethodToExpressionConverter staticMethodConverter) : ElementToExpressionConverter<Method>
    {
        protected override Expression Build(IBuilderContext context, Method method)
        {
            var parametersConverter = context.Builder.GetParametersConverter();
            switch (method.SourceType)
            {
                case MethodSource.Property:
                    return propertyMethodConverter.BuildInstance(context, method, parametersConverter);
                case MethodSource.Static:
                    return staticMethodConverter.BuildStatic(context, method, parametersConverter);
                case MethodSource.Constant:
                    break;
            }

            throw new InvalidMethodQueryException(method.GetName(context.HLinqQuery));
        }
    }
}