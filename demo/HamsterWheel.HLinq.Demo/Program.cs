using System.Text.Json.Serialization;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.Demo.Extensions.Translations.pl;
using HamsterWheel.HLinq.PgSql;
using HamsterWheel.HLinq.Request;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi()
    .ConfigureHLinq(c =>
    {
        c.AddHLingToPgSql();
        c.Extensions.AddTokenPossibility<SelectPossibility>();
        c.Extensions.AddTokenPossibility<HamsterWheel.HLinq.Demo.Extensions.Translations.math.OrderByDescendingPossibility>();
    })
    .AddDbContext<DemoContext>(o => { o.UseNpgsql(builder.Configuration.GetConnectionString("Demo")); });

builder.Services.AddControllers();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/demo/memory",
    (HLinqQuery<Superhero> query, CancellationToken cancellationToken) =>
        query.ApplyTo(Superhero.Superheroes.AsQueryable(), cancellationToken)).WithName("demo-memory");

app.MapGet("/demo/random",
    (HLinqQuery<RandomData> query, CancellationToken cancellationToken) =>
        query.ApplyTo(RandomData.Get().AsQueryable(), cancellationToken)).WithName("demo-random");

app.MapGet("/demo/db", async (HLinqQuery<Person> query, DemoContext demoContext, CancellationToken cancellationToken) =>
    {
        await demoContext.EnsureDbAndData(cancellationToken);
        return query.ApplyTo(demoContext.Persons, cancellationToken);
    })
    .WithName("demo-db");

app.UseExceptionHandler(_ => { });

app.Run();