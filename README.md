![Latest Release](https://g.np0.pl/hamster-wheel/hlinq/-/badges/release.svg) ![Status](https://g.np0.pl/hamster-wheel/hlinq/badges/master/pipeline.svg?ignore_skipped=true) ![Coverage](https://g.np0.pl/hamster-wheel/hlinq/badges/master/coverage.svg)


# Introduction

## Reference links

- [Hamster Wheel](https://internetexception.com/why-hamster-wheel/)
- 

# What's contained in this project

This project contains of two main parts:
- main package: HamsterWheel.HLinq that main implementation of HTTP Linq resource language
- HamsterWheel.HLinq.Abstractions package that can be used to further develop extensions for HLinq and adding functionalities missing from main package
- HamsterWheel.HLinq.AspNet package contains helpers for HLinq integration with Asp.NET core framework
- HamsterWheel.HLinq.PgSql package with PostgreSql specific functions for use in HLinq queries
- HamsterWheel.HLinq.Client package with fluent API for building HLinq queries on top of HttpClient


Navigation:
 - [How to use on server](#how-to-use-on-server)

# How to use on server

Below you can find instructions how to get you started using HLinq on the server using HamsterWheel.HLinq.AspNet package.

## Usage within minimal API

First, reference AspNet package:
```xml
<PackageReference Include="HamsterWheel.HLinq.AspNet" Version="0.4.0" />
```

After that add initialization of HLinq:
```csharp
services.ConfigureHLinq();
```

To actually make it useful you need to add it to the endpoint. In example if you fetch data from the database in this way:
```csharp
app.MapGet("/endpoint",
    (DbContext dbContext,CancellationToken cancellationToken) =>
    {
        return dbContext.MySet.Take(10).ToArrayAsync(cancellationToken);
    });
```

You need to add two services into this endpoint:
- `IHLinqQueryApplier` implements service that applies HLinq query to any implementation of `IQueryable`. Does not matter if this is in memory collection, enumerator or database set. If this is supported by Linq and EF it is supported by HLinq
- `HLinqQuery<MyEntity>` which is actual HLinq query mapped from HTTP Query String into type safe structure. `MyEntity` type is important to be the actual type you intend your users to query. 

So in above example it will become:
```csharp
app.MapGet("/endpoint",
    (DbContext dbContext, IHLinqQueryApplier applier, HLinqQuery<Superhero> query, CancellationToken cancellationToken) =>
    {
        var queryable = dbContext.MySet; 
        return applier.Apply(queryable, query, cancellationToken);
    });
```

And that is it!
You can now call this endpoint with HLinq query filters, paging and selects!

## Usage inside Controller

If you do not use minimal APIs of Asp.Net Core you can use HLinq within controllers too.
First you need to install package and configure it inside the host. Those are similar steps.


Reference AspNet package:
```xml
<PackageReference Include="HamsterWheel.HLinq.AspNet" Version="0.4.0" />
```

Initialize HLinq:
```csharp
services.ConfigureHLinq();
```

Then inject `IHlingQueryApplier` service into your controller:
```csharp
public class MyController(IHLinqQueryApplier applier) : ControllerBase
```

After that you can add `HLinqQuery<MyEntity>` into your endpoint method:
```csharp
HttpGet("my-endpoint")]
    public IActionResult GetData(HLinqQuery<MyEntity> query, CancellationToken cancellationToken) =>
        Ok(applier.Apply(dbContext.MySet, query, cancellationToken));
```

And that is all. Very similar to minimal APIs and also very simple.

# How to use on the client

HLinq libraries includes also client implementation that allows you to build HLinq queries in more type safe manner using `Expressio`s syntax of Linq. Not full Linq is supported in HLinq (mostly because limited support of URLs characters), but many of filtering methods, ordering, selects, skip, take directives are possible.
Using any specific client is not required though. You can use any HTTP client with Query String support.

## HTTP Queries
Lets use demo endpoint `/demo/memory` that returns `SuperHero` type data.

```http request
GET /demo/memory
```
returns data as is.

### Paging

Adding `take[10]` instructs HLinq to return only 10 records.
```http request
GET /demo/memory?take[10]
```

Adding `skip[20].take[10]` instructs HLinq to return only 10 records after skipping first 20 records.
```http request
GET /demo/memory?skip[20].take[10]
```

This allows for simple and expressive paging of data on the client side. I.e. if default page size is 10 rows, just call endpoint with:
```csharp
await httpClient.GetAsync($"/demo/memory?skip[{pageSize*page}].take[{pageSize}]");
```

### Filtering

There are various filters possible in HLinq:
- equals
- not equals
- greater
- greater or equal
- lesser
- lesser or equal
- string contains (with case-insensitive variant)
- string starts with (with case-insensitive variant)
- string ends with (with case-insensitive variant)

If this is not enough you can write your own extension to support new operators or functions.

### Equals
In example to look for entity with specific Id:
```http request
GET /data?where[x.Id==1]
### OR
GET /data?where[x.Id==77774169-BB9D-4DF9-A4A7-52019C4A445D]
```
Notice double `=` sign in comparison. It is consistent with how Linq works in C#. Single equals (`=`) sign is used in `select[NewName=x.Property]` only.



