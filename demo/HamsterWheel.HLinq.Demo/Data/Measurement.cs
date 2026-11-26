namespace HamsterWheel.HLinq.Demo.Data;

/// <summary>
/// The demo's rows of every numeric width, each with a nullable twin to compare against.
/// </summary>
public class Measurement
{
    public int Id { get; set; }
    public MeasurementKind Kind { get; set; }

    public string Name { get; set; } = null!;

    public int Level { get; set; }

    public short Short { get; set; }
    public byte Byte { get; set; }
    public ushort Ushort { get; set; }
    public int Int { get; set; }
    public uint Uint { get; set; }
    public long Long { get; set; }
    public ulong Unsigned { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public decimal Decimal { get; set; }
    public bool Flag { get; set; }
    public DateTime DateTime { get; set; }
    public MeasurementKind? NullableKind { get; set; }
    public short? NullableShort { get; set; }
    public byte? NullableByte { get; set; }
    public int? NullableInt { get; set; }
    public long? NullableLong { get; set; }
    public float? NullableFloat { get; set; }
    public double? NullableDouble { get; set; }
    public decimal? NullableDecimal { get; set; }
    public bool? NullableFlag { get; set; }
    public DateTime? NullableDateTime { get; set; }
}

public enum MeasurementKind
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3
}
