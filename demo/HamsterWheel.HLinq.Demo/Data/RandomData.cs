using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;

namespace HamsterWheel.HLinq.Demo.Data;

public class RandomData
{
    private static RandomData[]? _get;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsFullName()).Generate()!;

    public string Text { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsTextLipsum()).Generate() ?? "null";

    public bool Flag { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsBoolean()).Generate() ?? false;
    public bool? NullableFlag { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsBoolean()).Generate();
    public byte Byte { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public short Short { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public int Int { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public long Long { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;

    public RandomEnum Enum { get; set; } =
        (RandomEnum?)RandomizerFactory.GetRandomizer(new FieldOptionsInteger()).Generate() ?? RandomEnum.Longer;

    public decimal Decimal { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public double Double { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public float Float { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public byte? NullableByte { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public short? NullableShort { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public int? NullableInt { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;
    public long? NullableLong { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;

    public RandomEnum? NullableEnum { get; set; } =
        (RandomEnum?)RandomizerFactory.GetRandomizer(new FieldOptionsInteger()).Generate();

    public decimal? NullableDecimal { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;

    public double? NullableDouble { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;

    public float? NullableFloat { get; set; } = RandomizerFactory.GetRandomizer(new FieldOptionsByte()).Generate() ?? 0;

    public string? NullableString { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsTextLipsum()).Generate();

    public TimeOnly Time { get; set; } =
        TimeOnly.FromDateTime(RandomizerFactory.GetRandomizer(new FieldOptionsDateTime()).Generate() ?? DateTime.Now);

    public DateTime DateTime { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsDateTime()).Generate() ?? DateTime.Now;

    public DateTimeOffset DateTimeOffset { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsDateTime()).Generate() ?? DateTime.Now;

    public TimeOnly? NullableTime { get; set; } =
        TimeOnly.FromDateTime(RandomizerFactory.GetRandomizer(new FieldOptionsDateTime()).Generate() ?? DateTime.Now);

    public DateTime? NullableDateTime { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsDateTime()).Generate();

    public DateTimeOffset? NullableDateTimeOffset { get; set; } =
        RandomizerFactory.GetRandomizer(new FieldOptionsDateTime()).Generate();

    public static RandomData[] Get() => _get ??= Enumerable.Range(0, 100).Select(_ => new RandomData()).ToArray();
}

public class NestedRandom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
}

public enum RandomEnum
{
    Zero = 0,
    One = 1,
    Two = 2,
    Longer = 30,
    Negative = -1
}