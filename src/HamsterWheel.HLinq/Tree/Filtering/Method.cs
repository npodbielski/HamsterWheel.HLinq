using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class Method(IToken[] tokens) : TreeBranch(tokens)
{
    public MethodSource SourceType => Tokens.First() switch
    {
        Dot => _source ??= MethodSource.Property,
        NameOrValue => _source ??= MethodSource.Constant,
        _ => _source ??= MethodSource.Static
    };

    private MethodCall MethodCall => _method ??= Tokens.OfType<MethodCall>().First();
    private MethodSource? _source;
    private MethodCall? _method;

    public string GetName(string hLinqQuery) => MethodCall.GetValue(hLinqQuery);

    //TODO: move parsers and converters and token possibilities to IoC container instead of Activator, this way we will be able to inject other services into them
    public sealed class Converter : ElementToExpressionConverter<Method>
    {
        protected override Expression Build(IBuilderContext context, Method method)
        {
            var parametersConverter = context.Builder.GetParametersConverter();
            switch (method.SourceType)
            {
                //TODO: with change of parsers we could parse property as a child of method element that would make it significantly simpler
                case MethodSource.Property:
                {
                    var expression = context.MethodSource;
                    var propType = context.MethodSource!.Type;
                    var name = method.GetName(context.HLinqQuery);
                    var parameters = method.Children.OfType<IMethodParamElement>()
                        .Select(p => (p.GetValue(context.HLinqQuery),
                            p is Property property ? context.ToExpression(property) as MemberExpression : null))
                        .ToArray();
                    var methods = context.Builder.GetMostProbableMethods(propType, name, parameters);

                    MethodCallExpression? methodCallExpression = null;
                    foreach (var info in methods)
                    {
                        var expressions = parametersConverter.AttemptConversion(parameters,
                            info.GetParameters()
                                .Select<ParameterInfo, IBindingParameterInfo>(m => new BindingParameterInfo
                                    { Info = m }).ToArray());
                        try
                        {
                            methodCallExpression = Expression.Call(expression, info, expressions);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }

                    //$"Could not bound method '{name}' on the property {expression.Member.Name}"
                    return methodCallExpression ??
                           throw new InvalidMethodException(propType, context.MethodSource.Member.Name, name,
                               methods.Select(m => m.Name).ToArray());
                }
                case MethodSource.Static:
                {
                    var name = method.GetName(context.HLinqQuery);
                    var parameters = method.Children.OfType<IMethodParamElement>()
                        .Select(p => (p.GetValue(context.HLinqQuery),
                            p is Property property ? context.ToExpression(property) as MemberExpression : null))
                        .ToArray();
                    var methods = context.Builder.GetMostProbableMethods(name, parameters);
                    if (methods is not [])
                    {
                        MethodCallExpression? methodCallExpression = null;
                        foreach (var info in methods)
                        {
                            var expressions =
                                parametersConverter.AttemptConversion(parameters, info.parameters.ToArray());
                            try
                            {
                                methodCallExpression = Expression.Call(null, info.method, expressions);
                            }
                            catch (ArgumentException)
                            {
                            }
                        }

                        return methodCallExpression ??
                               throw new InvalidStaticMethodException(name,
                                   methods.Select(m => m.method.Name).ToArray());
                    }

                    throw new InvalidStaticMethodException(name,
                        context.Builder.GetMostProbableMethods(null, parameters).Select(m => m.method.Name).ToArray());
                }
                case MethodSource.Constant:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new NotImplementedException();
        }

        public sealed class InvalidMethodException(Type type, string propName, string method, string[] available)
            : HLinqQueryException(
                $"Invalid method for property '{type.Name}.{propName}.{method}'. Available methods at this point are: [{string.Join(", ", available)}]");

        public sealed class InvalidStaticMethodException(string method, string[] available)
            : HLinqQueryException(
                $"Invalid static method in query '{method}'. Available methods at this point are: [{string.Join(", ", available)}]");
    }

    public sealed class Parser : ElementParserBase<Method>
    {
        protected override Type[] ValidParents => [typeof(Condition)];

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightCircleBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens.Take(5).ToArray(),
                    [new RightCircleBracket(default)]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveTokensFromStart(1);
        }

        protected override Method? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Dot, MethodCall _, LeftCircleBracket, ..] => new Method(context.Tokens[..3]),
                [MethodCall _, LeftCircleBracket, ..] => new Method(context.Tokens[..2]),
                _ => null
            };
    }
}