using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
///     Differential tokenizer harness: pins the exact token stream (or throw type) of every query in
///     <see cref="TokenizerSnapshotData" />, asserted with the same
///     <c>HaveSequenceOf</c> style as the other tokenizer tests. The expectations are plain, manually
///     maintained C# — renaming a token type, matcher or exception is a compile error, never a silent
///     pass. Refactorings must keep the corpus untouched; when the
///     tokenizer behavior changes by design, edit the affected entries (and review the diff) as part
///     of that change. New syntax or features land together with their corpus entries.
/// </summary>
public class TokenizerSnapshotTests
{
    private readonly IHLinqTokenizer _sut = new HLinqTokenizer(TestServicesCollection.TokenPossibilities);
    private static readonly TestServicesCollection TestServicesCollection = new();

    [Fact]
    public void Tokenize_AllSnapshotQueries_ThenProducesTheRecordedTokenStreams()
    {
        var mismatches = new List<string>();
        foreach (var (query, throws, tokens) in TokenizerSnapshotData.Snapshots)
        {
            try
            {
                var actualTokens = _sut.Tokenize(query);
                if (throws is not null)
                {
                    mismatches.Add($"'{query}' was expected to throw {throws} but tokenized fine");
                }
                else
                {
                    actualTokens.Should().HaveSequenceOf(query, tokens!);
                }
            }
            catch (Exception e) when (throws is not null && e.GetType().Name == throws)
            {
                // the recorded throw for this query, as expected
            }
            catch (Exception e)
            {
                // assertion failures (wrong stream) and wrong/unexpected exceptions land here
                mismatches.Add($"'{query}': {e.Message}");
            }
        }

        mismatches.Should().BeEmpty(string.Join(Environment.NewLine + Environment.NewLine, mismatches));
    }
}
