# SCI Sales LLC - Senior .NET technical test

A small product catalog. Every read and write goes through a SQL Server stored
procedure, and one endpoint prices a product in another currency using a public
exchange rate API.

Stack: .NET 10, ASP.NET Core Web API (minimal APIs), SQL Server 2022, Dapper for
the stored procedure calls, xUnit v3 for the tests.

## What is in here

| Path | What it holds |
|---|---|
| `src/SciSales.Domain` | The Product entity, the Money and Currency value objects, and the rules they enforce. No dependencies on anything. |
| `src/SciSales.Application` | Use cases and the two ports they need: `IProductRepository` and `IExchangeRateProvider`. |
| `src/SciSales.Infrastructure` | The adapters: Dapper over stored procedures, and the HTTP client for the rate provider. |
| `src/SciSales.Api` | Endpoints, error mapping to RFC 9457 problem details, and the composition root. |
| `database/` | The four SQL scripts: database, table, stored procedures, seed data. |
| `tests/` | Domain, use case, architecture and API integration tests. |

Dependencies point inwards: Domain knows nobody, Application knows Domain,
Infrastructure and Api know Application. `tests/SciSales.ArchitectureTests`
fails the build if a project reference ever breaks that.

## Running it with Docker

This is the shortest path. It needs Docker, or Podman with the Docker CLI.

```bash
cp .env.example .env          # then edit MSSQL_SA_PASSWORD
docker compose up --build
```

Three services come up in order: SQL Server, a short-lived container that runs
the four scripts in `database/`, and the API. The API waits for the scripts to
finish, so the first request already finds the table and the procedures there.

- API: <http://localhost:8080>
- Interactive documentation: <http://localhost:8080/scalar/v1>
- SQL Server: `localhost,1433`, user `sa`, the password from your `.env`

To stop it and keep the data, `docker compose down`. To throw the database away
as well, `docker compose down -v`.

SQL Server is capped at 1 GB of memory through `MSSQL_MEMORY_LIMIT_MB`, with a
2 GB limit on the container. That is the lowest the image accepts: it checks for
2 GB at startup and exits if it finds less.

## Running it without Docker

You need the .NET 10 SDK and a SQL Server 2019 instance or newer.

1. Run the scripts in `database/` in numeric order with sqlcmd, SSMS or Azure
   Data Studio. All four are idempotent, so running them twice changes nothing.

   ```bash
   sqlcmd -S localhost -E -C -i database/01-create-database.sql
   sqlcmd -S localhost -E -C -i database/02-create-tables.sql
   sqlcmd -S localhost -E -C -i database/03-stored-procedures.sql
   sqlcmd -S localhost -E -C -i database/04-seed-data.sql
   ```

2. Put your connection string in `Database:ConnectionString` inside
   `src/SciSales.Api/appsettings.Development.json`. The file ships with Windows
   authentication against a local instance, which needs no password.

3. Start the API:

   ```bash
   dotnet run --project src/SciSales.Api
   ```

   It listens on <http://localhost:5163> and the documentation is at
   <http://localhost:5163/scalar/v1>.

## The database

One table, `dbo.Products`:

| Column | Type | Notes |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | Primary key |
| `Name` | `NVARCHAR(100)` | Unique index, so two products cannot share a name |
| `Description` | `NVARCHAR(500)` | Defaults to an empty string |
| `Price` | `DECIMAL(18,2)` | Check constraint: greater than zero |
| `CreatedDate` | `DATETIME2(3)` | Always UTC |

`CreatedDate` is `DATETIME2(3)` rather than the legacy `DATETIME`. Same idea,
wider range, and it round-trips a .NET `DateTimeOffset` converted to UTC without
losing precision.

Six stored procedures, and the API calls nothing else:

| Procedure | Used by |
|---|---|
| `usp_Products_Create` | POST /api/products |
| `usp_Products_GetById` | GET /api/products/{id} |
| `usp_Products_GetAll` | GET /api/products |
| `usp_Products_Count` | The paging metadata of that same GET |
| `usp_Products_Update` | PUT /api/products/{id} |
| `usp_Products_Delete` | DELETE /api/products/{id} |

They share three return codes: 0 for success, 1 for a duplicate name, 2 for a
row that is not there. The repository turns each one into the matching HTTP
status. The duplicate check sits on the unique index and is caught in a TRY
block, so two concurrent inserts cannot both win.

## The endpoints

| Method | Route | Answers |
|---|---|---|
| POST | `/api/products` | 201 with the created product, 400 on invalid input, 409 on a duplicate name |
| GET | `/api/products?page=1&pageSize=20` | 200 with one page, newest first |
| GET | `/api/products/{id}` | 200, or 404 |
| PUT | `/api/products/{id}` | 200 with the updated product, 400, 404 or 409 |
| DELETE | `/api/products/{id}` | 204, or 404 |
| GET | `/api/products/{id}/price?currency=COP` | 200 with the converted price, 400, 404 or 503 |
| GET | `/health` | 200 |

Errors come back as RFC 9457 problem details with an extra `code` field that is
stable and safe to branch on, for example `product.name.duplicate`. Exception
messages never reach the caller.

A create looks like this:

```bash
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Wireless Mouse","description":"Six-button ergonomic mouse.","price":24.99}'
```

## The public API it consumes

The catalog keeps its prices in USD. `GET /api/products/{id}/price?currency=COP`
asks <https://open.er-api.com> for the USD rate of the day and returns the
converted amount together with the rate used and when the provider last
refreshed it:

```json
{
  "productId": 1,
  "name": "Wireless Mouse",
  "basePrice": 24.99,
  "baseCurrency": "USD",
  "convertedPrice": 98716.25,
  "targetCurrency": "COP",
  "rate": 3950.25,
  "rateAsOf": "2026-09-20T00:02:31+00:00"
}
```

That provider needs no API key and publishes COP. It updates once a day, so the
answers are cached in memory for ten minutes; only successful ones, because a
failure has to be retried, not remembered. The HTTP client has a timeout and the
standard resilience handler on it, and if the provider is unreachable the
endpoint answers 503 instead of guessing a rate. Asking for USD is answered
without calling anybody.

Base address, timeout and cache window live in the `ExchangeRates` section of
`appsettings.json`.

## The tests

```bash
dotnet test
```

- `SciSales.Domain.Tests`: every rule of Product, Money and Currency, the valid
  case and each way of breaking it.
- `SciSales.Application.Tests`: each use case, the happy path and every failure
  it can return, with NSubstitute standing in for the two ports.
- `SciSales.ArchitectureTests`: the dependency rule, checked with NetArchTest.
- `SciSales.Api.IntegrationTests`: the real HTTP pipeline against a SQL Server
  started by Testcontainers, with the same four scripts applied to it. The only
  thing replaced is the exchange rate provider, because a test should not depend
  on a third party being up.

The integration tests need a container runtime. Without one they skip
themselves and say so rather than failing.

## Decisions worth explaining

**Dapper instead of EF Core.** The requirement is that every data operation goes
through a stored procedure. With EF Core that means `FromSqlRaw` everywhere and
a change tracker that has nothing to track. Dapper with
`CommandType.StoredProcedure` says exactly what happens.

**No mediator library.** Six use cases are six classes with one public method.
Adding MediatR would hide the call stack for no gain at this size.

**Validation lives in the entity.** `Product.Create` and `Product.Update` are
the only way to get a product, and they return a `Result` rather than throwing.
The endpoint maps that result to a status code. The database repeats the same
rules as constraints, because an application is not the only thing that can
write to a table.

**Nothing secret is committed.** `appsettings.Development.json` uses Windows
authentication, the Docker password comes from `.env`, and `.env` is
git-ignored.
