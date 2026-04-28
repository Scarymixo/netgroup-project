# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Event registration system (TalTech / NetGroup home assignment). .NET backend serving:
- MVC + Razor Pages UI (admin area, identity scaffolded UI)
- Versioned JSON REST API (`/api/v{version}/...`) consumed by a separate Vue frontend
- PostgreSQL via EF Core

Two roles: `admin` (can authenticate, create events) and `user` (anonymous; registers as a `Participant` to events). Admin credentials are seeded from configuration.

## Common commands

Run from solution root unless noted.

```bash
# Build / run
dotnet build
dotnet run --project WebApp

# EF Core migrations (App.DAL.EF is the migrations project, WebApp is the startup)
dotnet ef migrations --project App.DAL.EF --startup-project WebApp add <Name>
dotnet ef migrations --project App.DAL.EF --startup-project WebApp remove
dotnet ef database   --project App.DAL.EF --startup-project WebApp update
dotnet ef database   --project App.DAL.EF --startup-project WebApp drop
```

Scaffolding (run from inside `WebApp/`; requires `Microsoft.VisualStudio.Web.CodeGeneration.Design`). Disable warnings-as-errors before generating multiple controllers in one go, otherwise the second generation will fail to compile against the first.

```bash
# API controller
dotnet aspnet-codegenerator controller -name EventsController \
    -m App.Domain.Event -actions -dc AppDbContext \
    -outDir ApiControllers -api --useAsyncActions -f

# Identity UI re-scaffold
dotnet aspnet-codegenerator identity -dc DAL.App.EF.AppDbContext -f
```

Client-side libs are managed via LibMan (htmx, alpinejs).

No test project exists in the solution.

## Architecture

Layered N-tier solution (`NetGroupProject.sln`):

- **Base.Contracts.Domain / Base.Domain** — generic building blocks. `IBaseEntity` + `BaseEntity` (Guid `Id`). All domain entities inherit `BaseEntity`.
- **App.Domain** — domain entities (`Event`, `Participant`) and Identity types under `App.Domain.Identity` (`AppUser`, `AppRole`, `AppUserRole`, `AppRefreshToken`). `AppUserRole` is a custom join entity, so Identity is registered with the explicit `AddIdentity<AppUser, AppRole>` form (not `AddDefaultIdentity`).
- **App.DAL.EF** — `AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IDataProtectionKeyContext`. Also persists ASP.NET Data Protection keys to the DB. `Seeding/AppDataInit.cs` handles drop/migrate/seed orchestrated from `Program.cs`. EF migrations live here.
- **App.BLL** — currently a stub (`Class1.cs`). Reserved for service layer.
- **App.DTO** — versioned API contracts under `App.DTO.v1` (`EventRequests`, `ParticipantRequests`, `Identity/{LoginInfo,JWTResponse,TokenRefreshInfo,LogoutInfo}`, `RestApiErrorResponse`). New API versions go under `App.DTO.v2`, etc. Mappers (domain ↔ DTO) belong in `App.DTO/v1/Mappers`.
- **Base.Helpers** — `IdentityHelpers` (JWT generation etc.) reused across API controllers.
- **WebApp** — composition root. Contains:
  - `Program.cs` — DI, EF, Identity, JWT bearer + cookie auth, CORS (`CorsAllowAll`, exposes `X-Version` headers), API versioning with URL segment substitution (`v{version:apiVersion}`), Swagger per-version doc via `ConfigureSwaggerOptions`, and `SetupAppData` which waits for Postgres (`WaitDbConnection` retries until reachable) then runs drop/migrate/seed gated by `DataInitialization:*` flags in config.
  - `Controllers/` + `Views/` — public MVC (Home, Participants registration flow).
  - `Areas/Admin/` — admin-only MVC area for managing events.
  - `Areas/Identity/` — scaffolded Identity Razor Pages (login/register/etc.).
  - `ApiControllers/` — versioned JSON API. `ApiControllers/Identity/AccountController.cs` issues JWTs + refresh tokens.

### Auth model

- Cookie auth for the MVC/Razor admin UI.
- JWT bearer for the REST API. `JWT:Key/Issuer/Audience/ExpiresInSeconds` come from `appsettings.json`. `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap` is cleared so claim names are not remapped. `ClockSkew = TimeSpan.Zero`.
- Refresh tokens are persisted as `AppRefreshToken` entities (DbSet on `AppDbContext`).

### API versioning

`Asp.Versioning` is configured with URL segment versioning (`v{version:apiVersion}`), default `1.0`, `ReportApiVersions = true`. Swagger UI exposes a dropdown per discovered version via `IApiVersionDescriptionProvider`. When adding a new version, place controllers under a matching namespace and DTOs under `App.DTO.vN`.

### Database

- Postgres connection in `ConnectionStrings:DefaultConnection`. Startup will busy-wait until Postgres is reachable.
- `DataInitialization` flags in `appsettings.json` control drop/migrate/seed on each run — `DropDatabase: true` is set in the committed `appsettings.json`, so **starting the app currently wipes the DB**. Flip it off before doing anything you want to keep.
- EF is configured with split-query, no-tracking-with-identity-resolution, and `Throw(MultipleCollectionIncludeWarning)` — write queries accordingly (explicit `AsSplitQuery`/`AsTracking` is rarely needed; avoid multiple collection `Include`s in the same query).
- Unique constraint: `(Participant.EventId, Participant.NationalId)` — a person (by national ID) can register to an event at most once.

## Conventions

- `Directory.Build.props` enables `ImplicitUsings` and treats nullable warnings as errors across all projects. Honor nullability annotations; do not silence with `!` casually.
- Domain entities derive from `BaseEntity` (Guid `Id`). Identity entities use Guid keys to match.
- DTOs are version-namespaced (`App.DTO.v1.*`); never reference domain entities directly from API responses — go through the v1 mappers.