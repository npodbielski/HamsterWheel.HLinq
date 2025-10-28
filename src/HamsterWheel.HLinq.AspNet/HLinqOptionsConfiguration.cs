namespace HamsterWheel.HLinq.AspNet;

public class HLinqOptionsConfiguration : IHLinqOptions
{
    public int HttpDefaultMaxTakeRecords { get; set; } = HLinqOptions.DefaultMaxTakeRecords;
}