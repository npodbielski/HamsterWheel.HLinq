using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
/// <see cref="Grammar"/> is needed for <see cref="IHLinqTokenPossibility"/> direct injection make it circular dependency
/// </summary>
/// <param name="serviceProvider"></param>
// ReSharper disable once ClassNeverInstantiated.Global
public class GrammarDependencies(IServiceProvider serviceProvider)
{
    public IEnumerable<IHLinqTokenPossibility> TokenPossibilities =>
        field ??= serviceProvider.GetServices<IHLinqTokenPossibility>();
}