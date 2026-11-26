namespace HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

/// <summary>
/// Should contains all of the primitive types supported by EF https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/entity-data-model-primitive-data-types
/// </summary>
/// <param name="name"></param>
public class DummyEntity(string name) : IDummy
{
    public Guid Id { get; set; } = new();
    public string Name { get; set; } = name;
    public string Text { get; set; } = null!;

    /// <summary>
    /// A collection under the entity for an aggregate to fold.
    /// </summary>
    public List<NestedDummyEntity> Children { get; set; } = [];

    public bool Flag { get; set; }
    public bool? NullableFlag { get; set; }
    public byte Byte { get; set; }
    public ushort Ushort { get; set; }
    public short Short { get; set; }
    public int Int { get; set; }
    public uint Uint { get; set; }
    public long Long { get; set; }
    public ulong Unsigned { get; set; }
    public DummyEnum Enum { get; set; }
    public DummyByteEnum SmallEnum { get; set; }
    public decimal Decimal { get; set; }
    public double Double { get; set; }
    public float Float { get; set; }
    public byte? NullableByte { get; set; }
    public short? NullableShort { get; set; }
    public int? NullableInt { get; set; }
    public long? NullableLong { get; set; }
    public DummyEnum? NullableEnum { get; set; }
    public decimal? NullableDecimal { get; set; }
    public double? NullableDouble { get; set; }
    public float? NullableFloat { get; set; }
    public string? NullableString { get; set; }
    public TimeOnly Time { get; set; }
    public DateTime DateTime { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
    public TimeOnly? NullableTime { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public DateTimeOffset? NullableDateTimeOffset { get; set; }

    public NestedDummyEntity Nested { get; set; } = null!;
}

public class NestedDummyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public enum DummyEnum
{
    Zero = 0,
    One = 1,
    Two = 2,
    Longer = 30,
    Negative = -2
}

//an enum whose underlying type is not `int`: the meeting of an enum and a number is the
//number the enum is declared with, whatever width it is
public enum DummyByteEnum : byte
{
    Nothing = 0,
    Little = 3,
    Much = 44
}

public interface IDummy
{
    Guid Id { get; set; }
}

public class NameOnlyEntity
{
    public string Name { get; set; } = null!;
}