namespace HamsterWheel.HLinq.Pipeline.Applier;

public record Group<TKey, TRow>
{
    public TKey Key { get; init; } = default!;
    public List<TRow> Rows { get; init; } = [];
}
