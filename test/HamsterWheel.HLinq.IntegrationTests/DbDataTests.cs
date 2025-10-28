using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.IntegrationTests;

[Collection(nameof(IntegrationCollectionDefinition))]
public partial class DbDataTests(DemoFixture fixture)
{
    public DbSet<Person> Persons => fixture.PgSqlFixture.DemoContext.Persons;
}