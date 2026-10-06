# TrailCompanion API

A blank-slate .NET 10 API: four-layer Clean Architecture, Minimal APIs, MediatR, EF Core over PostgreSQL, and self-issued JWT auth. The only feature is `Users` (register, login, me). See [`CLAUDE.md`](./CLAUDE.md) for conventions.

## Prerequisites

- .NET SDK 10.0
- Docker
- `dotnet-ef`: `dotnet tool install --global dotnet-ef`

## Getting started

```bash
docker compose up -d
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/TrailCompanion.Api
dotnet ef database update --project src/TrailCompanion.Infrastructure --startup-project src/TrailCompanion.Api
dotnet run --project src/TrailCompanion.Api
```

The API listens on `http://localhost:5170`. In Development, Swagger UI is at `/swagger`.

## Try it

```bash
curl -X POST localhost:5170/api/users/register -H 'Content-Type: application/json' \
  -d '{"username":"me","password":"correct horse battery"}'

TOKEN=$(curl -s -X POST localhost:5170/api/users/login -H 'Content-Type: application/json' \
  -d '{"username":"me","password":"correct horse battery"}' | jq -r .accessToken)

curl localhost:5170/api/users/me -H "Authorization: Bearer $TOKEN"
```

## Endpoints

| Method | Route | Auth | Result |
| --- | --- | --- | --- |
| POST | `/api/users/register` | none | 201, 400 invalid input, 409 duplicate username |
| POST | `/api/users/login` | none, 5/min per IP | 200 `{ accessToken, expiresAtUtc }`, 401, 429 |
| GET | `/api/users/me` | bearer | 200, 401 |

## Not included (add when needed)

- **Refresh tokens / logout**: store a hashed random token in a `RefreshTokens` table with expiry; rotate on use.
- **Password reset**: needs an email or other recovery channel; deferred (TRAIL-11, TRAIL-12).
- **Roles**: add a `role` claim and `RequireAuthorization(p => p.RequireRole("admin"))`.
- **Background jobs**: Hangfire behind an `IBackgroundJobScheduler` contract in Application.
