using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.Demo.Data;

public class DemoContext(DbContextOptions<DemoContext> options) : DbContext(options)
{
    public DbSet<Person> Persons { get; set; }

    public DbSet<Address> Addresses { get; set; }

    public DbSet<Country> Countries { get; set; }

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