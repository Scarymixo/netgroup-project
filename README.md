# Netgroup Home Project

Event registration system. Admins create events; users (no login required) browse them and register with their personal details.

- **Backend public URL:** [misauk-netgroup-back.proxy.itcollege.ee](https://misauk-netgroup-back.proxy.itcollege.ee) (MVC, used for quick testing)
- **Frontend public URL:** [misauk-netgroup.proxy.itcollege.ee](https://misauk-netgroup.proxy.itcollege.ee) (Vue, separate repo)

## Overview

Two roles, only one of which authenticates:

- **Admin** — logs in with email/password, creates events (name, start/end time, max participants).
- **User** — anonymous; views the event list and registers for events with first name, last name and national ID code. May register for multiple events. Cannot cancel.

If no events exist, the list shows an empty-state message.

## Tech Stack

- **Backend:** ASP.NET Core (.NET) — clean architecture split into `App.Domain`, `App.DAL.EF`, `App.BLL`, `App.DTO`, `WebApp`
- **Frontend:** Vue (separate repository)
- **Database:** PostgreSQL (EF Core)
- **Auth:** JWT bearer tokens for the admin API

## Project Layout

| Project       | Role                                                                   |
|---------------|------------------------------------------------------------------------|
| `App.Domain`  | Entities (`Event`, `Participant`, identity types) and validation rules |
| `App.DAL.EF`  | EF Core `DbContext`, repositories, migrations, seed data               |
| `App.BLL`     | Business services                                                      |
| `App.DTO`     | Transport objects for the API                                          |
| `WebApp`      | ASP.NET host, MVC views, REST API controllers, Swagger                 |
| `TestProject` | Unit and integration tests                                             |
| `Base.*`      | Shared base classes for entities, repositories, helpers                |

## Decisions made

- **Layered architecture (Domain / DAL / BLL / DTO / WebApp).** Keeps EF Core out of the controllers and lets `App.BLL` own the registration rules independently of the transport (MVC views or REST API).
- **MVC views *and* a REST API in the same host.** `Controllers/` serves the server-rendered admin pages; `ApiControllers/` exposes the JSON API consumed by the Vue frontend. Swagger is enabled at `/swagger` for both manual exploration and integration tests.
- **JWT for the API, cookie auth for the MVC pages.** Admin is the only authenticated role, so the surface stays small; users are anonymous and never hit an auth-protected endpoint.
- **Capacity enforcement uses a serializable transaction with retry, not a `lock` or in-memory semaphore.** Two participants racing for the last slot is the tricky case — `ParticipantService.RegisterAsync` opens an `IsolationLevel.Serializable` transaction, re-reads the participant count, and retries up to 3 times on `40001`/`40P01`. The loser of the race surfaces as `EventFull` on re-read; a unique-violation (`23505`) becomes `DuplicateRegistration`. This pushes correctness into Postgres rather than relying on app-level locks that don't survive multiple instances.
- **Domain-level validation via `IValidatableObject`.** `Event.Validate` enforces "start at least 1h in the future" and "end after start" in the entity itself, so the rule holds whether the event is created via MVC or API.
- **National ID validated as exactly 11 digits.** Modeled with `[StringLength(11, MinimumLength = 11)]` plus a digits-only regex on `Participant.NationalId`.
- **Postgres + EF Core migrations, applied on startup.** `WaitDbConnection` blocks boot until the DB answers, then `MigrateDatabase`/`SeedIdentity`/`SeedData` flags in `appsettings.json` drive initialization. Makes local and container startup deterministic.
- **Seeded admin instead of a registration flow.** Per spec, only the admin authenticates, so credentials live in config/seed (`admin@taltech.ee` / `Kala.12345`) rather than a self-service signup.
- **Integration tests use `WebApplicationFactory` with seeding disabled** (`IntegrationTestFactory`) so each test controls its own fixture data.

## Getting Started

### Prerequisites

- .NET SDK (matching `Directory.Build.props`)
- PostgreSQL reachable on `localhost:5432` (default connection string in `WebApp/appsettings.json`)

### Run

```bash
dotnet build
dotnet run --project WebApp
```

The host blocks on startup via `WaitDbConnection` until Postgres responds. On first run, migrations are applied and seed data is loaded (controlled by `DataInitialization` flags in `appsettings.json`).

Once running:
- MVC UI: `https://localhost:<port>/`
- Swagger UI: `https://localhost:<port>/swagger`

### Default Admin Credentials (seeded)

```
email:    admin@taltech.ee
password: Kala.12345
```

Override seeding by editing `appsettings.json` → `DataInitialization`.

## Tooling

Install or update the .NET CLI tools used by this project:

```bash
dotnet tool update -g dotnet-ef
dotnet tool update -g dotnet-aspnet-codegenerator
dotnet tool update -g Microsoft.Web.LibraryManager.Cli
```

Restore client-side libraries (htmx, Alpine):

```bash
libman install htmx.org --files dist/htmx.min.js
libman install alpinejs --files dist/cdn.min.js
```

## Database Migrations

Run from the solution folder:

```bash
dotnet ef migrations --project App.DAL.EF --startup-project WebApp add Initial
dotnet ef migrations --project App.DAL.EF --startup-project WebApp remove
dotnet ef database   --project App.DAL.EF --startup-project WebApp update
dotnet ef database   --project App.DAL.EF --startup-project WebApp drop
```

## Tests

```bash
dotnet test
```

`TestProject` contains both unit tests and integration tests; integration tests spin up the host with seeding disabled (see `IntegrationTestFactory`).

## Configuration Reference

Key sections in `WebApp/appsettings.json`:

| Section                               | Purpose                                                              |
|---------------------------------------|----------------------------------------------------------------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection                                                |
| `DataInitialization`                  | Toggle `DropDatabase`, `MigrateDatabase`, `SeedIdentity`, `SeedData` |
| `JWT`                                 | Token signing key, issuer, audience, lifetime                        |