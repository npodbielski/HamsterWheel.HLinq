using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Grouping;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Grouping;
using HamsterWheel.HLinq.Tree.Ordering;
using HamsterWheel.HLinq.Tree.Paging;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq;

public static class ITreeBranchExtensions
{
    extension(ITreeBranch root)
    {
        public IReadOnlyList<T> ChildrenOfType<T>() where T : ITreeElement =>
            [.. root.Children.OfType<T>()];

        public T? GetChildOfType<T>() where T : ITreeElement =>
            root.Children.OfType<T>().SingleOrDefault();

        public string? GetTokenKeyword() => KeywordsByRoot.TryGetValue(root.GetType(), out var kind) ? kind : null;

        /// <summary>
        /// Returns true if tree branch changes client response type. In practice, it has to be either
        /// group or select
        /// </summary>
        public bool ChangesResponse() => root is SelectRoot or GroupByRoot;
    }

    private static string GetTokenKeyword<TWord>() where TWord : TokenWord, new() =>
        new TWord().Keyword ?? throw new InvalidOperationException($"{typeof(TWord).Name} have no keyword.");

    private static readonly Dictionary<Type, string> KeywordsByRoot = new()
    {
        [typeof(WhereRoot)] = GetTokenKeyword<Where.Word>(),
        [typeof(SelectRoot)] = GetTokenKeyword<Select.Word>(),
        [typeof(GroupByRoot)] = GetTokenKeyword<GroupBy.Word>(),
        [typeof(OrderByRoot)] = GetTokenKeyword<OrderBy.Word>(),
        [typeof(OrderByDescendingRoot)] = GetTokenKeyword<OrderByDescending.Word>(),
        [typeof(ThenByRoot)] = GetTokenKeyword<ThenBy.Word>(),
        [typeof(ThenByDescendingRoot)] = GetTokenKeyword<ThenByDescending.Word>(),
        [typeof(SkipRoot)] = GetTokenKeyword<Skip.Word>(),
        [typeof(TakeRoot)] = GetTokenKeyword<Take.Word>(),
        [typeof(CountRoot)] = GetTokenKeyword<Count.Word>(),
    };
}
