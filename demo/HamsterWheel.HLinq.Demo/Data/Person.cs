namespace HamsterWheel.HLinq.Demo.Data;

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? IpAddress { get; set; }
}