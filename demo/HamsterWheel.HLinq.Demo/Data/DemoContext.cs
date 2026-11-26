using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.Demo.Data;

public class DemoContext(DbContextOptions<DemoContext> options) : DbContext(options)
{
    public DbSet<Person> Persons { get; set; }

    public DbSet<Address> Addresses { get; set; }

    public DbSet<Country> Countries { get; set; }

    public DbSet<Measurement> Measurements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>()
            .HasOne(a => a.Country)
            .WithMany();
    }

    public async Task EnsureDbAndData(CancellationToken cancellationToken = default)
    {
        await Database.EnsureCreatedAsync(cancellationToken);
        if (Persons.FirstOrDefault() is null)
        {
            var assembly = typeof(DemoContext).Assembly;
            var jsonResource = assembly.GetManifestResourceNames().FirstOrDefault(r => r.Contains("persons.json"));
            var json = new StreamReader(assembly.GetManifestResourceStream(jsonResource!)!).ReadToEnd();
            var data = JsonSerializer.Deserialize<Person[]>(json, JsonSerializerOptions.Web);
            await Persons.AddRangeAsync(data!, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        if (Addresses.FirstOrDefault() is null)
        {
            await SeedAddressesAsync(cancellationToken);
        }

        if (Measurements.FirstOrDefault() is null)
        {
            //one row per shape of a nullable meeting its underlying member: equal values, a
            //nullable holding something else, and a null the lifted comparison answers no for;
            //the widths meet on some rows and differ on others, so a comparison of two columns
            //always has rows answering yes and rows answering no
            var rows = new List<Measurement>();
            for (var i = 1; i <= 9; i++)
            {
                var kind = (MeasurementKind)(i % 4);
                rows.Add(new Measurement
                {
                    Id = i,
                    Kind = kind,
                    Name = i % 2 == 0 ? kind.ToString() : ((int)kind).ToString(),
                    Level = i % 2 == 0 ? (int)kind : 100 + i,
                    Short = (short)(41 + i),
                    Byte = i % 2 == 0 ? (byte)(41 + i) : (byte)(39 + i),
                    Ushort = i % 2 == 0 ? (ushort)(41 + i) : (ushort)(300 + i),
                    Int = 3 + i,
                    Uint = i % 2 == 0 ? (uint)(41 + i) : (uint)(500 + i),
                    Long = i % 2 == 0 ? 3 + i : 5 + i,
                    Unsigned = (ulong)(10 * i),
                    Float = 3.5f + i,
                    Double = i % 2 == 0 ? 12.5 + i : 3.5 + i,
                    Decimal = 12.5m + i,
                    Flag = i % 2 == 0,
                    DateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                    NullableKind = i % 3 == 0 ? (MeasurementKind)(i % 4) : i % 3 == 1 ? null : (MeasurementKind)((i + 1) % 4),
                    NullableShort = i % 3 == 0 ? (short)(41 + i) : (short)(42 + i),
                    NullableByte = i % 3 == 0 ? (byte)(41 + i) : null,
                    NullableInt = i % 3 == 0 ? 3 + i : i % 3 == 1 ? null : 4 + i,
                    NullableLong = i % 3 == 0 ? 3 + i : i % 3 == 1 ? null : 4 + i,
                    NullableFloat = i % 3 == 0 ? 3.5f + i : null,
                    NullableDouble = i % 3 == 0 ? 3.5 + i : null,
                    NullableDecimal = i % 3 == 0 ? 12.5m + i : 13.5m + i,
                    NullableFlag = i % 3 == 0 ? i % 2 == 0 : null,
                    NullableDateTime = i % 3 == 0
                        ? new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i - 1)
                        : null
                });
            }

            rows.Add(new Measurement
            {
                Id = 10,
                Kind = MeasurementKind.Three,
                Name = "3",
                Level = 100,
                Short = 50,
                Byte = 50,
                Int = 12,
                Long = 12,
                Float = 3.5f,
                Double = 1e+16,
                Decimal = 9999999999999999.99m,
                Flag = true,
                DateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(10)
            });

            Measurements.AddRange(rows);
            await SaveChangesAsync(cancellationToken);
        }

        //the seed tracked the persons it walked and fixup filled their Addresses; left in the
        //tracker, the first request after a seed answers the related rows and every later
        //one does not - the answer of a query must not depend on whether this context seeded
        ChangeTracker.Clear();
    }

    private async Task SeedAddressesAsync(CancellationToken cancellationToken)
    {
        var towns = new[] { "Warsaw", "Krakow", "Gdansk" };
        Countries.AddRange(towns.Select((t, i) => new Country { Id = i + 1, Name = t }));
        await SaveChangesAsync(cancellationToken);

        var id = 1;
        foreach (var person in Persons)
        {
            Addresses.Add(new Address
            {
                Id = id++,
                PersonId = person.Id,
                Street = $"{person.FirstName} 1",
                City = "Warsaw",
                CountryId = 1
            });
            Addresses.Add(new Address
            {
                Id = id++,
                PersonId = person.Id,
                Street = $"{person.FirstName} 2",
                City = "Krakow",
                CountryId = 2
            });
        }

        await SaveChangesAsync(cancellationToken);
    }
}