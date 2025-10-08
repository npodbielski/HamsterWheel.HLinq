using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Selecting;

public sealed class InitializerPropertyName(NameOrValue name) : TreeLeaf([name])
{
    private string? _name;
    public string GetName(string hLinqQuery) => _name ??= Tokens.OfType<NameOrValue>().First().GetValue(hLinqQuery);

    public sealed class Parser : ElementParserBase<InitializerPropertyName>
    {
        protected override Type[] ValidParents { get; } = [typeof(PropertyAssignment)];

        protected override InitializerPropertyName? BuildBranch(IParsingContext context) =>
            context.Tokens is [NameOrValue name, Assignment, ..] ? new InitializerPropertyName(name) : null;
    }
}