namespace HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

/// <summary>
/// The rows of the nested select tests: a row with a collection of related rows and one single
/// related row, so both spellings of the call - over a collection and over a reference - are
/// exercised, and a related row holds a reference of its own for the depth of a call under a call.
/// </summary>
public class NestedPerson(string name)
{
    public string Name { get; set; } = name;
    public NestedAddress? Address { get; set; }
    public List<NestedAddress> Addresses { get; set; } = [];
}

public class NestedAddress
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public NestedCountry? Country { get; set; }
}

public class NestedCountry
{
    public string Name { get; set; } = null!;
}
