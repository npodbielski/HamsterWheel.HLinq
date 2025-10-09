using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public abstract class ElementParserBase<T> : IElementParser where T : ITreeElement
{
    protected virtual Type[] ValidParents { get; } = [];

    public bool ChildOf<TParent>(TParent parent) where TParent : ITreeElement =>
        ValidParents.Contains(parent.GetType());

    public Type ForElement() => typeof(T);

    public abstract IToken[] ExampleTokens { get; }

    public bool IsRoot => ValidParents.Length == 0;

    public void Finish(IParsingContext context) => FinishImpl(context);

    public bool TryBuildElement(IParsingContext context)
    {
        var newElement = BuildBranch(context);
        if (newElement is null)
        {
            return false;
        }

        context.Push(newElement);
        context.RemoveTokensFromStart(newElement.Tokens.Length);

        return true;
    }

    protected virtual void FinishImpl(IParsingContext context) => context.CurrentBranch?.Finish(context, []);

    protected abstract T? BuildBranch(IParsingContext context);
}