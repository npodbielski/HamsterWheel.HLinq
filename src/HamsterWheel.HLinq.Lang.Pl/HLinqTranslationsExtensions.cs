using System.Globalization;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Grouping;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Lang.Pl;

public static class HLinqTranslationsExtensions
{
    /// <summary>
    /// Adds Polish translation to HLinq: the words join the words every request is read with, so the
    /// endpoint keeps answering the queries it always answered. They carry no diacritics, a query living
    /// in a URL where <c>wez</c> and <c>weź</c> are one word only after somebody encoded it. Two words of
    /// a Polish query are not here - <c>null</c>, which is one word for a whole endpoint
    /// (<see cref="INullKeyword" />) rather than a word of a clause, and the alias of the entity, which
    /// the caller names.
    /// </summary>
    public static HLinqConfiguration AddPolishToHLinq(this HLinqConfiguration configuration)
    {
        var pl = CultureInfo.GetCultureInfo("pl");
        var extensions = configuration.Extensions;

        extensions.AddTranslatedKeyword<Where>(pl, "gdzie");
        extensions.AddTranslatedKeyword<Select>(pl, "wybierz");
        extensions.AddTranslatedKeyword<GroupBy>(pl, "grupujPo");
        extensions.AddTranslatedKeyword<Take>(pl, "wez");
        extensions.AddTranslatedKeyword<Skip>(pl, "pomin");
        extensions.AddTranslatedKeyword<Count>(pl, "zlicz");
        extensions.AddTranslatedKeyword<OrderBy>(pl, "sortujPo");
        extensions.AddTranslatedKeyword<OrderByDescending>(pl, "sortujPoMalejaco");
        extensions.AddTranslatedKeyword<ThenBy>(pl, "potemPo");
        extensions.AddTranslatedKeyword<ThenByDescending>(pl, "potemPoMalejaco");

        return configuration;
    }
}
