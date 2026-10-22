using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.AspNet;

/// <summary>
/// Validates HLinq grammar across all supported languages.
/// </summary>
internal class HLinqGrammarFilter(IServiceProvider services) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        GrammarValidator.Validate(services.GetRequiredService<IHLinqTokenizer>());
        return next;
    }
}