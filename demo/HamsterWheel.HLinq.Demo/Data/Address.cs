namespace HamsterWheel.HLinq.Demo.Data;

public class Address
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;

    public Country? Country { get; set; }

    public int? CountryId { get; set; }
}

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
