using System.Collections.Concurrent;
using System.Globalization;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
/// Processes query string and produces a sequence of tokens out of it. Tokenizer does not do backtracking. Just look ahead.
/// </summary>
public sealed partial class HLinqTokenizer : IHLinqTokenizer
{
    private readonly Language _invariant;
    private readonly Dictionary<string, Language> _byCulture;

    private static readonly ConcurrentDictionary<Type, Func<Range, IToken>> Builders = new();

    public HLinqTokenizer(IEnumerable<IHLinqWord> tokenWords)
    {
        var words = tokenWords.ToArray();

        _invariant = LanguageOf([.. words.Where(p => p.Culture is null)]);
        _byCulture = LanguagesOf(words);
    }

    public IToken[] Tokenize(string hLinqQuery, CultureInfo? culture = null)
    {
        if (culture is null || _byCulture.Count == 0)
        {
            return Process(hLinqQuery, _invariant);
        }

        for (var name = culture.Name; name.Length > 0; name = NearestCultureName(name))
        {
            if (_byCulture.TryGetValue(name, out var language))
            {
                return Process(hLinqQuery, language);
            }
        }

        return Process(hLinqQuery, _invariant);
    }

    internal Language[] GetLanguages() => [_invariant, .. _byCulture.Values];

    private static Dictionary<string, Language> LanguagesOf(IHLinqWord[] words)
    {
        var languages = new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase);

        foreach (var culture in words
                     .Where(p => p.Culture is not null)
                     .Select(p => p.Culture!.Name)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            languages[culture] = LanguageOf([
                .. words.Where(p =>
                    p.Culture is null
                    || string.Equals(p.Culture.Name, culture, StringComparison.OrdinalIgnoreCase))
            ]);
        }

        return languages;
    }

    private static Language LanguageOf(IHLinqWord[] words)
    {
        var grammar = new Grammar(words);

        WordTranslation[] all =
        [
            .. words.Select(w => new WordTranslation(w, w.PatternFor(grammar),
                grammar.GetRuleFor(w.ForType), BuilderFor(w.ForType)))
        ];

        var byClauseRoot = all
            .Select(w => w.Word.ForType)
            .Where(t => typeof(IClauseRootToken).IsAssignableFrom(t))
            .Distinct()
            .ToDictionary(root => root,
                root => all.Where(w => w.Rule.MatchesClauseRoot(root)).ToArray());

        var outsideOfAnyClause = all.Where(w => w.Rule.MatchesClauseRoot(null)).ToArray();

        return new Language(grammar, all, byClauseRoot, outsideOfAnyClause,
            [.. outsideOfAnyClause.Where(w => w.Rule.CanBeFirst)]);
    }

    private static string NearestCultureName(string culture)
    {
        var lastSubtag = culture.LastIndexOf('-');
        return lastSubtag < 0 ? "" : culture[..lastSubtag];
    }

    private static IToken[] Process(string hLinqQuery, Language language)
    {
        var span = hLinqQuery.AsSpan();
        var allWords = language.Words;
        var byClauseRoot = language.ByClauseRoot;
        var starting = language.Starting;
        List<IToken> tokens = [];
        var candidates = new WordTranslation[allWords.Length];
        var candidateCount = 0;
        var possible = new WordTranslation[allWords.Length];
        var possibleCount = 0;
        var previousTokenIndex = 0;
        var candidatesValid = false;
        var windowHasText = false;
        var clauseWords = language.OutsideOfAnyClause;

        for (var index = 0; index < span.Length; index++)
        {
            var window = span.Slice(previousTokenIndex, index - previousTokenIndex + 1);
            if (window.IsWhiteSpace())
            {
                //whitespace never starts a window, slide the window start over it
                previousTokenIndex++;
                candidatesValid = false;
                continue;
            }

            windowHasText = true;

            if (!candidatesValid)
            {
                candidateCount = 0;
                foreach (var word in tokens.Count == 0 ? starting : clauseWords)
                {
                    if (word.Rule.PreviousTokensMatch(tokens))
                    {
                        candidates[candidateCount++] = word;
                    }
                }

                candidatesValid = true;
            }

            var next = NextNonWhiteSpace(span, index);
            possibleCount = 0;
            WordTranslation? winner = null;
            var completeAt = 0;
            for (var candidate = 0; candidate < candidateCount; candidate++)
            {
                var match = candidates[candidate].Pattern.Match(window, next);
                if (match.IsDiscarded)
                {
                    continue;
                }

                possible[possibleCount++] = candidates[candidate];
                if (!match.IsCompleted || winner is not null)
                {
                    continue;
                }

                winner = candidates[candidate];
                completeAt = match.CompleteAt;
            }

            if (winner is null)
            {
                continue;
            }

            if (completeAt == 0)
            {
                throw new ZeroLengthCompletionException(winner.Word.ForType.Name);
            }

            var tokenEnd = previousTokenIndex + completeAt;
            var token = winner.Build(GetRange(previousTokenIndex..tokenEnd));
            tokens.Add(token);
            if (token is IClauseRootToken clauseRoot)
            {
                if (!byClauseRoot.TryGetValue(clauseRoot.GetType(), out var clause))
                {
                    clause = allWords;
                }

                clauseWords = clause;
            }

            previousTokenIndex = tokenEnd;
            index = tokenEnd - 1;
            candidatesValid = false;
            windowHasText = false;
            possibleCount = 0;
        }

        if (windowHasText)
        {
            throw new UnknownTokenException(hLinqQuery,
                [.. possible.Take(possibleCount).Select(w => w.Build(default))]);
        }

        return [.. tokens];
    }

    private static char? NextNonWhiteSpace(ReadOnlySpan<char> span, int windowEnd)
    {
        var nextCharIndex = windowEnd;
        char? next;
        do
        {
            next = nextCharIndex + 1 < span.Length ? span[nextCharIndex + 1] : null;
            nextCharIndex++;
        } while (next is not null && char.IsWhiteSpace(next.Value));

        return next;
    }

    private static Func<Range, IToken> BuilderFor(Type tokenType) =>
        Builders.GetOrAdd(tokenType, static type => (Func<Range, IToken>)typeof(TokenBase)
            .GetMethod(nameof(TokenBase.Create))!
            .MakeGenericMethod(type)
            .CreateDelegate(typeof(Func<Range, IToken>)));
}