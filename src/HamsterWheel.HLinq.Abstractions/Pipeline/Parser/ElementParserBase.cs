using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public abstract class ElementParserBase<T> : IElementParser where T : ITreeElement
{
    //the parse asks ChildOf; the set itself is public so a caller can read what a parser declares
    public virtual Type[] ValidParents { get; } = [];

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
        context.RemoveStartTokens(newElement.Tokens.Length);

        return true;
    }

    protected virtual void FinishImpl(IParsingContext context) => context.CurrentBranch?.Finish(context, []);

    protected abstract T? BuildBranch(IParsingContext context);
}