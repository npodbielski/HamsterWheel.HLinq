namespace HamsterWheel.HLinq.AspNet;

public class HLinqServicesConfiguration
{
    public IServiceProvider? ApiServices { get; set; }
    public List<Action<IServiceCollection>> Extensions { get; set; } = [];
    public HLinqOptionsConfiguration HLinqOptions { get; set; } = new();
}