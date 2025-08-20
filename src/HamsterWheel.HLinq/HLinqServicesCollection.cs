using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq;

public sealed class HLinqServicesCollection(IHLinqCore core) : IHLinqParsersCollection
{
    private IElementParser[]? _allParsers;
    private IElementToExpressionConverter[]? _allExpressionConverters;
    private IElementToMemberAssignmentConverter[]? _allMemberAssignmentConverters;
    private IHLinqTokenPossibility[]? _allTokenPossibilities;
    private readonly Dictionary<string, IElementParser[]> _extensionParsers = new();
    private readonly Dictionary<string, IElementToExpressionConverter[]> _expressionConverters = new();
    private readonly Dictionary<string, IElementToMemberAssignmentConverter[]> _memberAssignmentConverters = new();
    private readonly Dictionary<string, IHLinqTokenPossibility[]> _extensionTokenPossibilities = new();
    
    public IElementToExpressionConverter[] ExpressionConverters =>
        _allExpressionConverters ??= core.ExpressionConverters.Concat(_expressionConverters.SelectMany(kv => kv.Value)).ToArray();

    public IElementToMemberAssignmentConverter[] MemberAssignmentConverters =>
        _allMemberAssignmentConverters ??= core.AssignmentConverters.Concat(
            _memberAssignmentConverters.SelectMany(kv => kv.Value)).ToArray();

    public void Reset()
    {
        _allParsers = null;
        _allTokenPossibilities = null;
        _allExpressionConverters = null;
    }

    public IElementParser[] Parsers =>
        _allParsers ??= core.Parsers.Concat(_extensionParsers.SelectMany(kv => kv.Value)).ToArray();

    public IHLinqTokenPossibility[] Tokens => _allTokenPossibilities ??=
        core.TokenPossibilities.Concat(_extensionTokenPossibilities.SelectMany(kv => kv.Value)).ToArray();

    public void AddExtensionFromAssemblyWith<T>(string nameOfExtensions)
    {
        _extensionParsers[nameOfExtensions] = core.GetFromAssemblyWith<T, IElementParser>();
        _extensionTokenPossibilities[nameOfExtensions] = core.GetFromAssemblyWith<T, IHLinqTokenPossibility>();
        _expressionConverters[nameOfExtensions] = core.GetFromAssemblyWith<T, IElementToExpressionConverter>();
    }
}