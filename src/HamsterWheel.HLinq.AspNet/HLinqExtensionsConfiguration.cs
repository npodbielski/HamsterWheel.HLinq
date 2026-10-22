using System.Globalization;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.AspNet;

public class HLinqExtensionsConfiguration
{
    public List<Action<IServiceCollection>> CustomServices { get; } = [];

    /// <summary>
    /// Allows to add custom word to the grammar.
    /// </summary>
    public void AddWord<T>() where T : class, IHLinqWord =>
        CustomServices.Add(s => s.AddSingleton<IHLinqWord, T>());

    /// <summary>
    /// The same word, named the way a request names its culture.
    /// </summary>
    public void AddTranslatedKeyword<TToken>(CultureInfo culture, string keyword) where TToken : TokenBase, new()
        => CustomServices.Add(s =>
            s.AddSingleton<IHLinqWord>(new KeywordWord<TToken>(keyword, culture)));

    /// <summary>
    /// The same word, named the way a request names its culture.
    /// </summary>
    public void AddTranslatedKeyword<TToken>(string culture, string keyword) where TToken : TokenBase, new() =>
        AddTranslatedKeyword<TToken>(CultureInfo.GetCultureInfo(culture), keyword);

    /// <summary>
    /// A word taking the place of the word a token spells itself with, instead of standing next
    /// to it: the word registered here is the only one left that spells the token, in every language.
    /// </summary>
    public void OverWriteWord<TWord, TToken>() where TWord : class, IHLinqWord
    {
        CustomServices.Add(s =>
        {
            var originalImplementation = s.First(sd =>
            {
                if (sd.ImplementationType?.BaseType?.IsGenericType != true)
                {
                    return false;
                }

                var baseType = sd.ImplementationType?.BaseType;
                return baseType?.GetGenericTypeDefinition() == typeof(TokenWord<>)
                       && baseType.GenericTypeArguments[0] == typeof(TToken);
            });
            s.Remove(originalImplementation);
            s.AddSingleton<IHLinqWord, TWord>();
        });
    }

    public void AddDbFunctions<T>() where T : class, IStaticMethodSource =>
        CustomServices.Add(s => s.AddSingleton<IStaticMethodSource, T>());

    internal void InstallAll(IServiceCollection services)
    {
        foreach (var service in CustomServices)
        {
            service(services);
        }
    }
}