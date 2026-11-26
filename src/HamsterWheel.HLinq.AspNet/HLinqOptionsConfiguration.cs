namespace HamsterWheel.HLinq.AspNet;

public class HLinqOptionsConfiguration : IHLinqOptions
{
    public int HttpDefaultMaxTakeRecords { get; set; } = HLinqOptions.DefaultMaxTakeRecords;

    public bool EmitWarnings { get; set; } = true;

    public int MaximumQueryLength { get; set; } = HLinqOptions.DefaultMaximumQueryLength;

    public int MaximumNumberOfProperties { get; set; } = HLinqOptions.DefaultMaximumNumberOfProperties;
}
