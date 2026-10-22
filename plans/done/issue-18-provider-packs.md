# Provider packs: MySQL, MariaDB and SQLite

## Goal

Expose independently installable MySQL, MariaDB, and SQLite function packs without changing HLinq's core binder.

## Design

- `HamsterWheel.HLinq.MySql` and `HamsterWheel.HLinq.MariaDb` each publish an `IStaticMethodSource` and configuration extension, while intentionally sharing Pomelo's generic EF, standard MySQL, and JSON extension classes.
- `HamsterWheel.HLinq.Sqlite` publishes the generic EF extension class and an `AddSqliteToHLinq()` configuration extension.
- The packs target `net10.0`, reference `HamsterWheel.HLinq.AspNet`, and use centrally managed Pomelo and EF Core SQLite packages.

## Callable surface

What a query reaches in each engine, and what stays out of its reach and why, is in
[docs/db-functions](../../docs/db-functions/README.md). `ProviderFunctionSurfaceUnitTests` records the
names the binder is called with, so no count of them is repeated here: a count written down here is
a count that goes stale the moment the binder binds one name more.

## Validation

- Unit tests cover source types, fluent registration, and `Like` expression binding for all three packs.
- A reflection and builder probe records the provider counts and currently bindable names.
- The solution and both unit-test projects must build and pass before merge.