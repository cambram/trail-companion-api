# TrailCompanion API

.NET 10 Minimal API backing a trail-race planner app. Clean Architecture, MediatR CQRS, EF Core 10 on PostgreSQL 17, self-issued JWT auth. A runner registers, logs in, and plans races as ordered segments, each holding fuel items (carbs) used to total planned versus needed carbs against a per-hour target. Frontend lives in a separate repo; auth contract for clients is in `docs/auth.md`.

# Domain

- `AppUser`: the account. JWT `sub` = `AppUser.Id`.
- `Races`: owned by `AppUserId`. `Name`, `Target` (carbs per hour, default 60), nullable `StartTime` (`TimeOnly`) and `RaceDate` (`DateOnly`, no timezone).
- `Segments`: belong to a race. `Position`, `Name`, `Duration`, `EndKm?` (cumulative km mark where the segment ends; length is derived on the client), `Notes`.
- `Items`: belong to a segment, reference a `NutritionItems`, carry `Position`. They hold no nutrition values of their own.
- `NutritionItems`: the user's nutrition library, owned by `AppUserId`. `Name` (unique per user), `Type` (`Gel`, `Liquid`, `Solid`, `Pill`, stored as text), nullable `Size` and macros (`Carbs`, `Calories`, `Caffeine`, `Sodium`, `Potassium`, `Magnesium`, `Calcium`). `Sodium`, `Potassium`, `Magnesium` and `Calcium` are `decimal?` at `HasPrecision(8, 2)`; the rest are `int?`. `Size`'s unit is implied by `Type`. Null means unknown, not zero.
- `AidStations`: belong to a segment (the segment they precede), at most one per segment via the deferrable `UQ_AidStations_SegmentId`. `Name`, `Notes`, `Kit` (jsonb list of `Label` + `Certainty` `Planned`/`Maybe`). No duration. Moving into an occupied segment swaps.
- `AidStationItems`: belong to an aid station, reference a `NutritionItems`, carry `Position`. Their carbs count toward the station's segment on the client.
- `Races` and `NutritionItems` carry `AppUserId`. Segments and items are owned through the chain: handlers verify race and segment via repositories that take `appUserId`.
- Entity classes are plural (`Races`, `Segments`, `Items`). Race features currently live in the `Users` module folders across all layers.
- A library item in use cannot be deleted: `DeleteNutritionItemCommand` calls `CountUsagesAsync`, which counts `Items` and `AidStationItems`, and throws `ConflictException`. Both foreign keys are `Restrict`.
- Routes: see `Endpoints/ApiEndpoints.cs` and the table in `docs/auth.md`. Nested: `/api/races/{raceId}/segments/{segmentId}/items`. Library: `/api/nutrition-items`.
- `docs/race-reads-option-a-single-endpoint.md` documents the nested `GET /api/races/{id}` DTO design.

# Build & Development Commands

```bash
docker compose up -d                            # postgres:17 as trailcompanion-db
dotnet build                                    # ZERO errors, ZERO warnings
dotnet test                                     # integration tests need Docker
dotnet test --filter "Category!=Integration"    # unit only, no Docker
dotnet run --project src/TrailCompanion.Api     # http://localhost:5170, Swagger at /swagger
```

- Single test: `dotnet test --filter "FullyQualifiedName~LoginUserCommandHandlerTests"`. A filter matching nothing exits 0; confirm the reported count.
- Add migration: `dotnet ef migrations add <Name> --project src/TrailCompanion.Infrastructure --startup-project src/TrailCompanion.Api --output-dir Persistence/Migrations`
- Apply: `dotnet ef database update --project src/TrailCompanion.Infrastructure --startup-project src/TrailCompanion.Api`
- JWT key (min 32 bytes, API refuses to start without it): `dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/TrailCompanion.Api`

# Post-Generation Workflow

1. `dotnet build`: zero errors, zero warnings.
2. `dotnet test`: all green.
3. Entity or `IEntityTypeConfiguration` changed: generate migration.
4. Report what actually ran and what it returned.

# Architectural Patterns

```
Api -> Infrastructure -> Application -> Domain
Api -> Application
```

- `src/TrailCompanion.Domain`: entities, exceptions, repository interfaces. **Zero references.**
- `src/TrailCompanion.Application`: handlers, DTOs, contracts (`IPasswordHasher`, `IAccessTokenIssuer`), `ICurrentUser`, `JwtOptions`.
- `src/TrailCompanion.Infrastructure`: `ApplicationDbContext`, EF configurations, repositories, migrations, hashing, token issuing.
- `src/TrailCompanion.Api`: `Program.cs` composition root, endpoints, middleware, JWT validation, rate limiting.
- Tests: `Application.Tests` (xUnit + NSubstitute), `Infrastructure.Tests` (EF in-memory, hasher), `Infrastructure.IntegrationTests` (Testcontainers Postgres, fresh migrated DB per class), `Api.Tests` (`WebApplicationFactory` + Testcontainers). Tag every Docker-backed test `[Trait("Category", "Integration")]`.
- `Common/` is organised by technical kind, plural noun (`Contracts/`, `Services/`, `Entities/`, `Exceptions/`, `Middleware/`, `Constants/`, `Extensions/`, `Configuration/`). Never a business concept.
- `Modules/<Module>/` is organised by business capability. Module folder name is identical across Domain, Application, and `Endpoints/<Module>/` in Api.
- Infrastructure groups by technical concern: `Persistence/Configuration/<Module>/`, `Repositories/<Module>/`, `Authentication/`, `Persistence/Migrations/`.
- Each project root has `GlobalUsings.cs` and an empty `I<Layer>AssemblyMarker.cs`; assembly scanning targets the marker. Never add a namespace to `GlobalUsings.cs` that generated migrations already import (`Microsoft.EntityFrameworkCore`): CS8933.
- DI: one static class per concern in `Common/Configuration/DependencyInjection/<Concern>Configuration.cs`, composed by `AddApplication()`, `AddInfrastructure(IConfiguration)`, `AddApi()`. Registrations are explicit (new repository = one line in `RepositoryConfiguration`). Only endpoints and MediatR handlers are found by reflection.
- CQRS via MediatR. No pipeline behaviours; validation is explicit in the handler.
- Middleware order is load-bearing: `CorrelationId` -> `ExceptionHandling` -> CORS -> `UseRateLimiter()` -> `UseAuthentication()` -> `UseAuthorization()` -> endpoints.

# Code Style & Conventions

**Requests and handlers**
- Request classes: `sealed`, values via constructor, get-only properties. Never positional records.
- Handlers: `internal sealed <RequestName>Handler`, same file as the request, in `Commands/` or `Queries/`.
- Shared request validation goes in an `internal static <Thing>RequestRules` class (see `NutritionItemRequestRules`) that throws `ValidationFailedException`.
- DTOs are records in `Modules/<Module>/Dtos/`, with a static `FromEntity` where needed.
- Handlers throw domain exceptions; `ExceptionHandlingMiddleware` maps them: `ValidationFailedException`/`DomainException` 400, `UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `ConflictException` 409, else 500. All `ProblemDetails` with `correlationId`.

**Endpoints**
- One `internal sealed` class per endpoint implementing `IEndpoint`, in `Endpoints/<Module>/<Verb><Noun>Endpoint.cs`. A class without `IEndpoint` is silently skipped; if a route 404s, check that first.
- Routes are constants in `ApiEndpoints.cs`, nested per module under `api`. No raw path strings.
- Call `RequireAuthorization()` or `AllowAnonymous()` on each endpoint. No route group inherits either. Verify new endpoints return 401 unauthenticated.
- `WithName(Name)` with `Name = nameof(TheEndpointClass)`; `WithTags(Modules.X)` from `Common/Constants/Modules.cs`.
- Bodies map only: no validation, no ownership checks, no `try`/`catch`. Injected services carry `[FromServices]`.

**Ownership**
- Endpoint supplies `appUserId` from `ICurrentUser`, never from route or body.
- Handlers take `appUserId` and filter on it. Ownership is enforced in the handler, not the endpoint.
- Cross-user read throws `NotFoundException` (404, never 403).

**Persistence and layering**
- Domain: plain POCOs, no framework attributes. Persistence rules live in `IEntityTypeConfiguration<T>`.
- Every entity inherits `BaseEntity`; `ApplicationDbContext` stamps `CreatedAtUtc`/`UpdatedAtUtc`.
- Only shared entities get a declared `DbSet`. Feature entities enter via `ApplyConfigurationsFromAssembly` and are reached through `Context.Set<T>()`.
- Application reaches identity through `ICurrentUser`, never `HttpContext`.
- Unique index violations become `ConflictException` via `Repository.SaveChangesAsync` and `UniqueConstraintConflicts`. Add new unique index names there.

**Auth**
- PBKDF2-HMAC-SHA256, 600,000 iterations, stored `iterations.salt.hash`. HS256 JWT, 15 minute expiry, no ASP.NET Core Identity.
- Refresh tokens: opaque, SHA-256 hashed in `RefreshTokens`, rotated on every use with reuse detection, delivered only in the `HttpOnly` `refresh_token` cookie. Endpoints read and write the cookie via `RefreshTokenCookie`; commands see plain strings, never `HttpContext`.
- Password rules live in `PasswordRules`, shared by register and change password.
- Login returns the same 401 for unknown username and wrong password and always hashes (`IPasswordHasher.Verify(password, null)`).
- Usernames go through `UserName.Normalize` (trim, lowercase) before storage and lookup; register also checks `UserName.IsValid`.
- Rate limits (`RateLimitingConfiguration`, fixed window per IP): login 5/min, refresh 30/min, change password and delete account 5/min.
- Never put secrets or personal data in the JWT. HTTPS only outside local dev.

**Observability**
- Every request gets a correlation id: inbound `X-Correlation-Id` if it passes `CorrelationContext.Sanitize`, else a fresh one. Echoed on the response and in `ProblemDetails.correlationId`. Logging scopes use message templates.

# Adding a Feature

Module names are PascalCase plural nouns, reused verbatim in every layer.

1. Domain: `Modules/<Module>/Entities/` (inherit `BaseEntity`, `AppUserId` if owned) and `Repositories/I<Entity>Repository.cs` whose reads take `appUserId`.
2. Application: `Modules/<Module>/Dtos|Commands|Queries/`, one file per operation, scoped by `appUserId`.
3. Infrastructure: `Persistence/Configuration/<Module>/` and `Repositories/<Module>/` (inherit `Repository`, use `Context.Set<T>()`); register in `RepositoryConfiguration`.
4. Api: route constants in `ApiEndpoints`, a `Modules` tag, one `IEndpoint` per route.
5. Tests, then migration. Handler tests build the handler with NSubstitute mocks and call `Handle(request, CancellationToken.None)`; one file per handler, `<RequestName>HandlerTests.cs`.

# Guardrails

- Do not run `git commit`, `git push`, or anything that writes git history. The user commits.
- Do not write comments in code: no `//`, `/* */`, XML docs, including `.csproj` and `.json`. Naming carries meaning.
- Do not add any reference to `TrailCompanion.Domain`.
- Do not reference `TrailCompanion.Infrastructure` anywhere except `Program.cs`.
- Do not reference ASP.NET Core from `TrailCompanion.Application`.
- Do not add a `DbSet<T>` for a feature entity, or a navigation collection on `AppUser` pointing at one.
- Do not edit or delete a pushed migration. Fix mistakes with a new one.
- Do not trust the EF in-memory provider for DB guarantees (unique indexes, lengths, nullability, transactions). Use integration tests.
- Do not put real secrets or connection strings in `appsettings.json`. User-secrets only.
