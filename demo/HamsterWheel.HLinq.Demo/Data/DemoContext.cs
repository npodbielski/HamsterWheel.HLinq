using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.Demo.Data;

public class DemoContext(DbContextOptions<DemoContext> options) : DbContext(options)
{
    public DbSet<Person> Persons { get; set; }

    public async Task EnsureDbAndData(CancellationToken cancellationToken = default)
    {
        await Database.EnsureCreatedAsync(cancellationToken);
        var person = Persons.FirstOrDefault();

        if (person is null)
        {
            var assembly = typeof(DemoContext).Assembly;
            var jsonResource = assembly.GetManifestResourceNames().FirstOrDefault(r => r.Contains("persons.json"));
            var json = new StreamReader(assembly.GetManifestResourceStream(jsonResource!)!).ReadToEnd();
            var data = JsonSerializer.Deserialize<Person[]>(json, JsonSerializerOptions.Web);
            await Persons.AddRangeAsync(data!, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }
    }
}