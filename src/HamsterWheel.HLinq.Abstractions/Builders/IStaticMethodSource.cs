namespace HamsterWheel.HLinq.Builders;

public interface IStaticMethodSource
{
    /// <summary>
    /// Types with static methods that will be attempted to bind in HLinq query (i.e. 'where[someStaticMethod(x.Property, someValue)'] 
    /// </summary>
    Type[] Types { get; }
}