# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# TrailCompanion API: .NET 10 / Clean Architecture / JWT auth / PostgreSQL

## 1. Project Overview

A blank-slate API template. The only feature is the `Users` module: register, log in, and read the current user. It exists to demonstrate every convention below; new features copy its shape.

## 2. Technical Stack & Architecture

- **Language/Framework**: C# / .NET 10 (`net10.0`), ASP.NET Core Minimal APIs. No controllers.
- **Database**: PostgreSQL 17 via EF Core 10 (Npgsql), Docker Compose for local dev.
- **Auth**: self-issued JWTs. Passwords are hashed with PBKDF2-HMAC-SHA256 (600,000 iterations, random salt, stored as `iterations.salt.hash`). Login signs an HS256 token with `sub = AppUser.Id` and a 15-minute expiry. `JwtBearer` validates signature, expiry, issuer and audience. No refresh tokens, no ASP.NET Core Identity.
- **Business logic**: CQRS via MediatR. Each operation is a `Commands/` or `Queries/` request class paired with its `IRequestHandler` in the same file. Endpoints resolve `ISender` and `Send()` the request. No MediatR pipeline behaviours; validation is explicit in the handler.

```
TrailCompanion.Api → TrailCompanion.Infrastructure → TrailCompanion.Application → TrailCompanion.Domain
TrailCompanion.Api → TrailCompanion.Application
```

- `src/TrailCompanion.Domain`: Entities, exceptions, repository interfaces. **Zero references.**
- `src/TrailCompanion.Application`: Command/query handlers, DTOs, contracts (`IPasswordHasher`, `IAccessTokenIssuer`), `ICurrentUser`, `JwtOptions`.
- `src/TrailCompanion.Infrastructure`: `ApplicationDbContext`, EF configurations, repositories, migrations, password hashing and token issuing.
- `src/TrailCompanion.Api`: `Program.cs` composition root, endpoints, middleware, JWT validation, rate limiting.
- `tests/`: `TrailCompanion.Application.Tests` (xUnit + NSubstitute), `TrailCompanion.Infrastructure.Tests` (EF in-memory, password hasher), `TrailCompanion.Infrastructure.IntegrationTests` (real PostgreSQL via Testcontainers, fresh migrated database per test class), `TrailCompanion.Api.Tests` (end-to-end HTTP via `WebApplicationFactory` against Testcontainers). Tag every Docker-backed test `[Trait("Category", "Integration")]`.

### 2.1 `Common/` vs `Modules/`

- **`Common/`** is organised by *technical kind*, always a plural noun: `Contracts/`, `Services/`, `Entities/`, `Exceptions/`, `Middleware/`, `Schemas/`, `Constants/`, `Extensions/`, `Configuration/`. Never a business concept.
- **`Modules/<Module>/`** is organised by *business capability*, and the module folder name is **identical across Domain and Application** (and `Endpoints/<Module>/` in Api).
- **Infrastructure groups by technical concern first**: `Persistence/Configuration/<Module>/`, `Repositories/<Module>/`, `Authentication/`. `Persistence/Migrations/` holds migrations.

Each project root carries a `GlobalUsings.cs` and an empty `I<Layer>AssemblyMarker.cs`. Assembly scanning targets the marker. **Do not add a namespace to `GlobalUsings.cs` that generated migration files already import** (notably `Microsoft.EntityFrameworkCore`): the duplicate raises CS8933.

**DI is one static class per concern**, in `Common/Configuration/DependencyInjection/<Concern>Configuration.cs`, composed by one per-layer extension in `Common/Configuration/Extensions/`: `AddApplication()`, `AddInfrastructure(IConfiguration)`, `AddApi()`. Registrations are explicit: a new repository adds a line to `RepositoryConfiguration`. Endpoints and MediatR handlers are the only things discovered by reflection.

## 3. Essential Commands

```bash
docker compose up -d          # postgres:17 as trailcompanion-db
dotnet build                  # must be clean with ZERO warnings
dotnet test                   # integration tests need Docker
dotnet test --filter "Category!=Integration"    # unit only, no Docker
dotnet run --project src/TrailCompanion.Api           # http://localhost:5170, Swagger at /swagger
```

Single test: `dotnet test --filter "FullyQualifiedName~LoginUserCommandHandlerTests"`. A filter matching nothing still exits 0; confirm the reported count.

Migrations:

```bash
dotnet ef migrations add <Name> --project src/TrailCompanion.Infrastructure \
  --startup-project src/TrailCompanion.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/TrailCompanion.Infrastructure --startup-project src/TrailCompanion.Api
```

Secrets go to user-secrets, never a committed file. The API refuses to start without a signing key of at least 32 bytes:

```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/TrailCompanion.Api
```

## 4. Post-Generation Workflow

1. **Build**: `dotnet build`, zero errors and zero warnings.
2. **Test**: `dotnet test`, all green.
3. **Migration**: if an entity or `IEntityTypeConfiguration` changed, generate the migration now.
4. **Verify**: report what actually ran and what it returned.

## 5. Coding Standards

**Ownership model**:

- `AppUser` is the account. The JWT `sub` claim is `AppUser.Id`; `ICurrentUser.AppUserId` reads it.
- Owned entities carry an `AppUserId` foreign key.
- Handlers take `appUserId` and filter on it. **Ownership is enforced in the handler, not the endpoint.** The endpoint supplies it from `ICurrentUser`, never from the route or body.
- A cross-user read throws `NotFoundException` → **404, not 403**. 403 would confirm another user's record exists.

**Auth rules**:

- Login returns the same 401 for unknown email and wrong password, and always runs the hash (`IPasswordHasher.Verify(password, null)` hashes against a dummy) so timing does not reveal whether an email exists.
- Emails are trimmed and lowercased before storage and lookup (`UserEmail.Normalize`). Uniqueness is a DB unique index; `Repository.SaveChangesAsync` translates the violation to `ConflictException` via `UniqueConstraintConflicts`.
- The login route has a fixed-window rate limit (5 per minute per IP, `RateLimitingConfiguration`).
- Never put secrets or personal data in the JWT. HTTPS only outside local dev.

**Layering**:

- Domain: plain POCOs, no framework attributes. Persistence rules live in `IEntityTypeConfiguration<T>`.
- Application reaches HTTP identity through `ICurrentUser`, never `HttpContext`, and never references ASP.NET Core.
- Only shared entities get a declared `DbSet`. Feature entities enter the model via `ApplyConfigurationsFromAssembly` and are reached through `Context.Set<T>()`.
- Every entity inherits `BaseEntity`; `ApplicationDbContext` stamps `CreatedAtUtc`/`UpdatedAtUtc`.

**Requests and handlers**:

- Request classes are `sealed`, take values through the constructor, and expose get-only properties. Never a positional record.
- Handlers are `internal sealed <RequestName>Handler`, in the same file as the request.
- Request/response DTOs live in `Modules/<Module>/Dtos/` as records, with a static `FromEntity` mapper where needed.
- Handlers throw domain exceptions; `ExceptionHandlingMiddleware` translates them: `ValidationFailedException`/`DomainException` → 400, `UnauthorizedException` → 401, `NotFoundException` → 404, `ConflictException` → 409, else 500. All `ProblemDetails` with `correlationId`.

**Endpoints**:

- **One class per endpoint**, `internal sealed`, implementing `IEndpoint`. Files live in `Endpoints/<Module>/`, named `<Verb><Noun>Endpoint.cs`.
- **Routes are constants** in `Endpoints/ApiEndpoints.cs`, nested per module under the `api` base. No raw path strings.
- `MapApiEndpoints()` is the single registration call. A class that does not implement `IEndpoint` is **silently skipped**; if a new route 404s, check the interface first.
- Protected endpoints call `RequireAuthorization()` individually; public ones call `AllowAnonymous()`. No route group inherits either. Verify new endpoints return 401 unauthenticated.
- `WithName(Name)` where `Name = nameof(TheEndpointClass)`; `WithTags(Modules.X)` from `Common/Constants/Modules.cs`.
- Endpoint bodies map and nothing else: no validation, no ownership checks, no `try`/`catch`.
- Injected services carry explicit `[FromServices]`.

**Observability**: every request gets a correlation id: inbound `X-Correlation-Id` if it passes `CorrelationContext.Sanitize`, otherwise a fresh one. It is echoed on the response and appears in `ProblemDetails.correlationId`. Logging scopes use message templates.

**Middleware order is load-bearing**: `CorrelationId` → `ExceptionHandling` → CORS → `UseRateLimiter()` → `UseAuthentication()` → `UseAuthorization()` → endpoints.

**Adding a module** (PascalCase plural noun, reused verbatim in every layer):

1. `TrailCompanion.Domain/Modules/<Module>/Entities/` (inherit `BaseEntity`, add `AppUserId` if owned) and `Repositories/I<Entity>Repository.cs` whose reads take `appUserId`.
2. `TrailCompanion.Application/Modules/<Module>/Dtos/`, `Commands/`, `Queries/`: one file per operation, scoped by `appUserId`.
3. `TrailCompanion.Infrastructure/Persistence/Configuration/<Module>/` and `Repositories/<Module>/` (inherit `Repository`, use `Context.Set<T>()`); register in `RepositoryConfiguration`. Add any unique index name to `UniqueConstraintConflicts`.
4. `TrailCompanion.Api`: route constants in `ApiEndpoints`, a `Modules` tag, one `IEndpoint` class per route.
5. Tests, then migration. Handler tests construct the handler with NSubstitute mocks and call `Handle(request, CancellationToken.None)`; one file per handler, `<RequestName>HandlerTests.cs`.

## 6. Guardrails

- **Do not run `git commit`, `git push`, or anything that writes git history.** The user commits.
- **Do not write comments in code.** No `//`, `/* */`, or XML doc comments, including `.csproj` and `.json` files. Code explains itself through naming.
- **Do not add any reference to `TrailCompanion.Domain`.**
- **Do not reference `TrailCompanion.Infrastructure` anywhere except `Program.cs`.**
- **Do not reference ASP.NET Core from `TrailCompanion.Application`.**
- **Do not add a `DbSet<T>` for a feature entity**, or a navigation collection on `AppUser` pointing at one.
- **Do not edit or delete a pushed migration.** Correct mistakes with a new one.
- **Do not trust the EF in-memory provider for database guarantees** (unique indexes, lengths, nullability, transactions). Those belong in integration tests.
- **Do not put real secrets or connection strings in `appsettings.json`.** User-secrets only.
