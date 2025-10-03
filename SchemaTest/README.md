# SchemaTest

Proof-of-concept that wires a .NET 9 Aspire distributed application with Entity Framework Core 9 and a MySQL container.

## Prerequisites

- [.NET SDK 9 preview](https://dotnet.microsoft.com/download/dotnet/9.0)
- Docker Desktop (or another OCI-compatible container runtime) running and accessible

## Projects

- `SchemaTest.AppHost` &mdash; Aspire host that provisions a MySQL container and orchestrates the API.
- `SchemaTest.Api` &mdash; Minimal API that exposes CRUD endpoints backed by Entity Framework Core and MySQL.
- `SchemaTest.ServiceDefaults` &mdash; Shared Aspire defaults (health checks, telemetry, resilience).

## Running the POC

```pwsh
cd SchemaTest
# spin up the distributed application (MySQL container + API)
dotnet run --project SchemaTest.AppHost
```

Once the application is ready, the Aspire dashboard prints the public URL for the API (typically `http://localhost:8080`). Import the OpenAPI document at `/openapi/v1.json` into your preferred client.

### Sample requests

```pwsh
# list customers
Invoke-RestMethod -Uri 'http://localhost:8080/customers'

# create a customer
Invoke-RestMethod -Uri 'http://localhost:8080/customers' -Method Post -Body (@{ name = 'Alan Turing'; email = 'alan@example.com' } | ConvertTo-Json) -ContentType 'application/json'

# check connectivity against MySQL
Invoke-RestMethod -Uri 'http://localhost:8080/db-check'
```

The minimal hosted service seeds a couple of sample customers on first run so you can immediately validate persistence.

## Database schema

A single `customers` table is created automatically via `EnsureCreated`:

- `Id` (INT, identity)
- `Name` (nvarchar 120)
- `Email` (nvarchar 160)
- `CreatedAt` (timestamp, defaults to current UTC value)

Feel free to add EF Core migrations as you evolve the schema; this POC keeps things lightweight by seeding data programmatically.
