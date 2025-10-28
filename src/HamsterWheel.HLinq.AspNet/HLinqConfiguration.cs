namespace HamsterWheel.HLinq.AspNet;

public class HLinqConfiguration
{
    public HLinqExtensionsConfiguration Extensions { get; } = new();
    public HLinqOptionsConfiguration HLinqOptions { get; } = new();
}