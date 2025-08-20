using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.PgSql;
using HamsterWheel.HLinq.Request;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi()
    .ConfigureHLinq(o => o.AddHLingToPgSql())
    .AddDbContext<DemoContext>(o => { o.UseNpgsql(builder.Configuration.GetConnectionString("Demo")); });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/demo/memory",
    (IHLinqQueryApplier applier, HLinqQuery<Superhero> query, CancellationToken cancellationToken) =>
        applier.Apply(Superhero.Superheroes.AsQueryable(), query, cancellationToken)).WithName("demo-memory");

app.MapGet("/demo/random",
    (IHLinqQueryApplier applier, HLinqQuery<Superhero> query, CancellationToken cancellationToken) =>
        applier.Apply(RandomData.Get().AsQueryable(), query, cancellationToken)).WithName("demo-random");

app.MapGet("/demo/db", async (IHLinqQueryApplier applier, HLinqQuery<Person> query, DemoContext demoContext,
        CancellationToken cancellationToken) =>
    {
        await demoContext.EnsureDbAndData(cancellationToken);
        return applier.Apply(demoContext.Persons, query, cancellationToken);
    })
    .WithName("demo-db");

app.UseExceptionHandler(_ => { });

app.Run();