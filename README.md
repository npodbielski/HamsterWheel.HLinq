![Latest Release](https://internetexception.com/wp-content/uploads/hlinq-badges/release.svg) ![Status](https://internetexception.com/wp-content/uploads/hlinq-badges/pipeline.svg) ![Coverage](https://internetexception.com/wp-content/uploads/hlinq-badges/coverage.svg)


# Introduction

## Reference links

- [Hamster Wheel](https://internetexception.com/why-hamster-wheel/)
- [HLinq design decisions]()

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
    (DbContext dbContext, CancellationToken cancellationToken) =>
    {
        return dbContext.MySet.Take(10).ToArrayAsync(cancellationToken);
    });
```

You need to add `HLinqQuery<MyEntity>` model into your endpoint which is actual HLinq query mapped from HTTP Query String into type safe structure. `MyEntity` type is important to be the actual type you intend your users to query. 

So in above example it will become:
```csharp
app.MapGet("/endpoint",
    (DbContext dbContext, HLinqQuery<Superhero> query, CancellationToken cancellationToken) =>
    {
        var queryable = dbContext.MySet; 
        return query.ApplyTo(queryable, cancellationToken);
    });
```

And that is it!
You can now call this endpoint with HLinq query filters, ordering, paging and selects!

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

Then you must add `HLinqQuery<MyEntity>` into your endpoint method:
```csharp
[HttpGet("my-endpoint")]
public IActionResult GetData(HLinqQuery<MyEntity> query, CancellationToken cancellationToken) =>
    Ok(query.ApplyTo(dbContext.MySet, cancellationToken));
```
And that is all. Very similar to minimal APIs and also very simple.

## Configuring HLinq inside API

### Default limit of records returned
If you are using HLinq entirely within bounds of one application, there is no default limit of how many records you will fetch. And it is not really a problem. You can do that too with just Linq to SQL, i.e. call `ToArray` or `ToList` on entire DB collection. But on the API side you do not really have control on what yours users will do and calling and API endpoint without any query modifiers, me personally it is first thing I do, just to see what kind of result it will return, with what information. Since HLinq requires `skip[].take[]` to be provided this would cause entire data to be serialized and sent to the client. At once. This would be very problematic. Of course this is also not desired by 99% of cases. To fix this HLinq limits records returned to maximum value of 1k. If you do not like the default limit you can change it:
```csharp
builder.Services.ConfigureHLinq(c => c.HLinqOptions.HttpDefaultMaxTakeRecords = 100);
```
This will limit number of records returned by default to 100. 

### Overriding user requested number of records
If a user is trying to fetch a huge quantity of data from the API by specifing i.e. `take[1000000]`, this may overload your db and API server(s). To remedy this, HLinq is overriding those values with `IHLinqOptions.HttpDefaultMaxTakeRecords` value. If you want to specify a different value for that case change this inside `ConfigureHLinq` method:
```csharp
builder.Services.ConfigureHLinq(c => c.HLinqOptions.HttpDefaultMaxTakeRecords = 100);
```

### Adding EF DB Functions
In Linq you can use DB functions from inside the C# code. Those functions are not evaluated on the .net side but translated in SQL and evaluated by the DB engine. HLinq allows you to use them, but it is not aware of your environment.
To tell HLinq about your DB capabilities, you need to configure it inside `ConfigureHLinq` method call.
For example, the following line adds PgSql specific functions to HLinq:
```csharp
services.ConfigureHLinq(c => c.AddHLingToPgSql());
```

# How to use on the client

HLinq have a client that allows you to build HLinq queries in a more type safe manner using `Expression`s syntax of Linq. Not full Linq is supported in HLinq (mostly because limited support of URLs characters), but many of filtering methods, ordering, selects, skip, take directives are possible.
Using any specific client is not required, though. You can use any HTTP client with Query String support.

## Syntax

HLinq syntax is very similar to Linq syntax. It contains roots, that are equivalent to Linq methods (like `Where`, `Select`, `OrderBy`, `Skip`, `Take`) which are parametrized by providing `[]` with appropriate arguments values. 
Supported roots are:
- `where`
- `select`
- `skip`
- `take`
- `count`
- `orderBy`
- `orderByDescending`
- `thenBy`
- `thenByDescending`

Roots are chained together using `.` character. For example:
```http request
GET where[x.Name==John].skip[10].take[20].select[fullName=x.Name]
```

Casing in root names is not important. For example, `WHERE` and `where` are equivalent. Or example even `wHeRe` is valid to harder to read.
The same applies to property names, though JSON property names are preferred. So for example `x.firstName` is preferred when `x.FirstName` is also possible and it is a valid CLR property name.

Roots order matters. For example, if you are querying `Person` type that have `FirstName` property, querying the API with `where[x.firstName==John].select[name=x.firstName]` will first filter the collection selecting all the persons with `firstName` being `John` and then will select only `firstName` property as new `name` property. Resulting json will be:
```json
[
  {
    "name": "John"
  }
]
```
On the other hand, when you will query API with `select[name=x.firstName].where[x.firstName==John]` query will return an error.
```json
{
  "title": "Invalid property path 'firstName' for entity 'DynamicAnonymousType0`1'. Available properties at this point are: name ",
  "status": 400
}
```
This is because `firstName` is property of `Person` type, but when you applying `select[name=x.firstName]` you are effectively changing `IQueryable` to being a collection of new type. C# equivalent of this query would be:
```csharp
personQueryable.Select(x => new { Name = x.FirstName });
```
As you can see, `FirstName` does not exist in a queryable collection anymore.

Parameters of roots depend on the root. 

For example, `count` root does not take any parameters. The only valid usage is `count[]`.

`take` and `skip` roots single number as a parameter (i.e. `take[10]`, `skip[20]`).

`orderBy` and `orderByDescending` roots take a single parameter, which is a property name to order by.

`select` root takes unspecified number of parameters, separated by `,`, parameters with two variants:
- `select[x.name]` selects property as is
- `select[newName=x.name]` renames selected property.

`select` allow to select as many properties with both. The order of properties or renames is not important.

`where` root also takes unspecified number of parameters. Parameters are separated by `,`. Syntax depends on the following:
- type of property
- comparison operator or comparison method
- value you are comparing to

For example, when you are comparing to constant value:
```http request
GET /data?where[x.{property name}{operator}{constant value}]
```
But if you want to filter a property containing a specific value:
```http request
GET /data?where[x.{property name}.Contains({constant value})]
```
The same can be achieved with an EF method call:
```http request
GET /data?where[ilike(x.{property name},{constant value})]
```
or the same can be achieved with `string.Contains(property, StringComparison.InvariantCultureIgnoreCase)` (or similar instance of a property type) method:
```http request
GET where[x.{property name}.{property type method}({constant value},{constant argument})]
```
Constant value within where parameters can be almost any value. How it is treated depends on the type of the property. I.e. `int` will be converted to `int` before comparison. String will not be converted and will be taken as is from Query String. 

It is possible to use () in `where`. This effectively allows grouping of conditions. For example 
```http request
GET where[(x.Id==1||x.Name.StartsWith(d))&&x.DateOfBith>=2010-08-31 00:00]
```
will return either record with Id=1 or records with Name starting with `d` when date of birth is after 2010-08-31 00:00.
But
```http request
GET where[x.Id==1||x.Name.StartsWith(d)&&x.DateOfBith>=2010-08-31 00:00]
```
will return either with Id=1 OR records with Name starting with `d` AND date of birth is after 2010-08-31 00:00.
This works the same as in Linq.

Constant values do not need to be quoted. If you are looking for a person with their full name, put space in a string between first and last name:
```http request
GET where[x.firstName==John Doe]
```

### White space management
White space is not important in HLinq. You can almost use any white space you want. It is mostly ignored by the parser, except for constant values provided by the user. 
For example, leading or trailing white space before or after the root is allowed.
```csharp
"    orderBy[x.Name]          "
```
You can put white space between root and `[` character:
```csharp
"orderBy  [x.Name]"
```
Only space is allowed, but other white space characters are allowed also.
For example:
```csharp
"orderBy[x\r\n.Name]"
```
This means that for longer queries you can use new lines to make it more readable.
```csharp
"""
orderBy[x.Name]
    .skip[10]
    .take[100]
"""
```

## HTTP Queries
Lets use demo endpoint `/demo/memory` that returns `SuperHero` type data.

```http request
GET /demo/memory
```
returns data as is, which one exception being the maximum limit of records returned (by default 1k).

### Paging

Adding `take[10]` instructs HLinq to return only 10 records.
```http request
GET /demo/memory?take[10]
```

Adding `skip[20].take[10]` instructs HLinq to return only 10 records after skipping first 20 records.
```http request
GET /demo/memory?skip[20].take[10]
```

This allows for simple and expressive paging of data on the client side. I.e., if the default page size is 10 rows, call the endpoint with:
```csharp
await httpClient.GetAsync($"/demo/memory?skip[{pageSize*page}].take[{pageSize}]");
```

To count total number of records in the collection you are querying you need to use `count[]` root.
```http request
GET /demo/memory?count[]
```

Count can be applied after filtering (or any other root for that matter) to get only the number of records in a filtered collection instead.
```http request
GET /demo/memory?where[x.name.contains(Ant)].count[]
```
This will give you the number of records in the collection that have `name` property containing `Ant` value.

### Filtering

There are various filters possible in HLinq:
- equals
- not equals
- greater
- greater or equal
- lesser
- lesser or equal
- string contains (with case-insensitive variant) - usually not supported by DB
- string starts with (with case-insensitive variant) - usually not supported by DB
- string ends with (with case-insensitive variant) - usually not supported by DB
- EF DbFunctions

If this is not enough, you can write your own extension to support new operators or functions.

#### Equals
In example to look for entity with specific Id:
```http request
GET /data?where[x.Id==1]
```
```http request
GET /data?where[x.Id==77774169-BB9D-4DF9-A4A7-52019C4A445D]
```
```http request
GET /data?where[x.Name==John]
```
```http request
GET /data?where[x.Name==John Smith]
```
Notice double `=` sign in comparison. It is consistent with how Linq works in C#. Single equals (`=`) sign is used in selectors (`select[NewName=x.Property]`) only.

To filter by records that do not have value use null keyword:
```http request
GET /data?where[x.Name==null]
```

To filter by floating point numeric you can use `.` delimiter for fractions. It is consistent with `CultureInfo.InvariantCulture` numerical format, which HLinq is using by default.
```http request
GET where[x.Number==1.2323]
```
HLinq does not apply any precision for equality comparison, so due to floating point rounding errors you may get different results. To fix this you can use two values with `>` and `<` operators.
```http request
GET where[x.Number>1.2322&&x.Number<1.2324]
```

For Date Time and Date Time Offset types you can use ISO 8601 format:
```http request
GET where[x.DateOfBirth==2025-10-05T19:43:07.3693705Z]
```
Or with the time zone part:
```http request
GET where[x.Modified==2025-10-05T19:44:10.8405723+00:00]
```
Equals can be used inside a group as any other filtering condition:
```http request
GET where[(x.Name==John||x.Name==Doe)]
```

#### Not equals
Usage is the same as for equals, but with `!=` operator.
```http request
GET /data?where[x.Id!=1]
### OR
GET /data?where[x.Id!=77774169-BB9D-4DF9-A4A7-52019C4A445D]
```
Not equals comparison operator is `!=` which is consistent with C# syntax of Linq.

#### Greater
Usage is the same as for equals, but with `>` operator.
```http request
GET /data?where[x.Id>1]
```

#### Greater or equal
Usage is the same as for equals, but with `>=` operator.
```http request
GET /data?where[x.Int>=1]
```

#### Lesser
Usage is the same as for equals, but with `<` operator.
```http request
GET /data?where[x.Int<1]
```

#### Lesser or equal
Usage is the same as for equals, but with `<=` operator.
```http request
GET /data?where[x.Int<=1]
```

#### String contains
Testing for string contents is a bit different but still resembles Linq syntax. 
```csharp
queryable.Where(x => x.Name.Contains("John"));
```
In HLinq you have to query it like below instead:
```http request
GET /data?where[x.Name.Contains(John)]
```
You can use the white space in the string:
```http request
GET /data?where[x.Name.Contains(John Doe)]
```

#### String contains with case-insensitive
> [!WARNING]  
> Though this is supported by HLinq and Linq, it is not supported by EF. Use it for memory collections only. 

Of course usage of `string.Contains` is consistent with Linq and is case-sensitive.
To use a case-insensitive version, you need to add an extra parameter just like in C#:
```http request
GET /data?where[x.Name.Contains(John, StringComparison.InvariantCultureIgnoreCase)]
```
Also, it is possible to use DB functions in case-insensitive searches:
```http request
GET /data?where[ilike(x.Name, n)]
```
This is equivalent of a Linq query:
```csharp
queryable.Where(x => x.Name.Contains("n", StringComparison.InvariantCultureIgnoreCase));
```

#### String StartsWith
> [!WARNING]  
> Though this is supported by HLinq and Linq, it is not supported by EF. Use it for memory collections only.

In the same manner as `Contains` can be used `StartsWith` method:
```http request
GET /data?where[x.Name.StartsWith(John)]
```
To use a case-insensitive version, you need to add an extra parameter just like in C#:
```http request
GET /data?where[x.Name.StartsWith(John, StringComparison.InvariantCultureIgnoreCase)]
```

#### String EndsWith
> [!WARNING]  
> Though this is supported by HLinq and Linq, it is not supported by EF. Use it for memory collections only.

In the same manner as `Contains` and `StartsWith` can be used `EndsWith` method:
```http request
GET /data?where[x.Name.EndsWith(n)]
```
To use a case-insensitive version, you need to add an extra parameter just like in C#:
```http request
GET /data?where[x.Name.EndsWith(John, StringComparison.InvariantCultureIgnoreCase)]
```

#### EF DbFunctions
> [!WARNING]  
> Support for those functions vary between DB engines and their EF providers. Check which ones are by your DB engine and EF.

> [!WARNING]  
> This is supported by HLinq and Linq and EF but cannot be used on memory collections. Use it for DB collections only.

##### PGSQL 
###### ILike
This is very similar to `string.Contains(str, StringComparison.InvariantCultureIgnoreCase)` but instead of using .net runtime function it is translated to db function, and it is applied by DB engine.
For example:
```http request
/demo/db?where[ilike(x.NAME, John Doe)]
```
This is equivalent of a Linq query:
```csharp
queryable.Where(x => EF.Functions.ILike(x.Name, "John Doe"));
```

###### ILike with escape character
Very similar to regular ILike but allows for a third argument: escape character.
```http request
GET /demo/db?where[ilike(x.firstName, arlan, \)]
```

This is equivalent of a Linq query:
```csharp
queryable.Where(x => EF.Functions.ILike(x.Name, "John Doe", "\\"));
```

### Selecting
Selecting is used to specify which properties should be returned to the client. It is done by specifying a comma-separated list of property names after the `select` keyword. It is possible to rename properties by specifying new name with `newName=propertyName` syntax.
For example:
```http request
GET /data/persons?select[x.Name,x.Age]
```

This is equivalent of a Linq query:
```csharp
personQueryable.Select(x => new { x.Name, x.Age });
```

This will return only `Name` and `Age` properties of each person only.

If you want to rename `Name` property to `FullName` then you do:
```http request
GET /data/persons?select[fullName=x.name,x.age]
```

This, in turn, usually translates to a Linq query as follows:
```csharp
personQueryable.Select(x => new { FullName = x.Name, x.Age });
```

There is possibility to add constant value to the property:
```http request
GET /demo/memory?select[Name=1]
```
It can be useful when real property was filtered by select, but a client requires it, i.e., for serialization. It can be also used to achieve compatibility on the client when a server data model is changed without changing the actual code.

HLinq creates a new type on the fly to be serialized to JSON and returns it to the client. This means that other properties of the original type are not returned.
Selecting is not an exclusive operation. You need to be specific by providing which properties you are interested in. You cannot exclude properties by excluding them from the query.

### Ordering
If you need to fetch data in a specific order, you can use `orderBy` and `orderByDescending` roots.
This orders a collection by `Name` property in ascending order:
```http request
GET /demo/memory?orderBy[x.Name]
```
There reverse order use `orderByDescending` root instead.
```http request
GET /demo/memory?orderByDescending[x.Name]
```
It is possible to order by more than one property at a time.
To do this use any number of `thanBy` or `thanByDescending` roots after `orderBy` or `orderByDescending` roots.
```http request
GET /demo/memory?orderBy[x.Name].thenBy[x.realName]
```





# Using 