# Authentication

How auth works in TrailCompanion API. Part 1 is for anyone working on the API. Part 2 is for anyone building a client (the frontend repo).

## TL;DR

- Self-issued JWT bearer tokens. No ASP.NET Core Identity, no OAuth provider.
- Register with email + password, log in to get an access token, send it as `Authorization: Bearer <token>`.
- Access token lives 15 minutes. There is **no refresh token**. When it expires, the user logs in again.
- The token carries one claim that matters: `sub` = the user's `AppUser.Id` (GUID).
- Every endpoint except register and login requires a valid token.

---

# Part 1: Backend

## Endpoints

Base path is `api`. Route constants live in `src/TrailCompanion.Api/Endpoints/ApiEndpoints.cs`.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/users/register` | Anonymous | Create an account |
| POST | `/api/users/login` | Anonymous, rate limited | Exchange credentials for an access token |
| GET | `/api/users/me` | Bearer required | Return the caller's account |
| GET, POST | `/api/races` | Bearer required | List / create races |
| GET, DELETE | `/api/races/{id}` | Bearer required | Read / delete a race |
| POST | `/api/races/{raceId}/segments` | Bearer required | Create segment |
| PUT, DELETE | `/api/races/{raceId}/segments/{id}` | Bearer required | Update / delete segment |
| POST | `/api/races/{raceId}/segments/{segmentId}/items` | Bearer required | Attach a library item to a segment |
| DELETE | `/api/races/{raceId}/segments/{segmentId}/items/{id}` | Bearer required | Delete item |
| GET, POST | `/api/nutrition-items` | Bearer required | List / create nutrition library item |
| PUT, DELETE | `/api/nutrition-items/{id}` | Bearer required | Update / delete nutrition library item |

All `{id}` style route params are constrained to GUIDs.

### `POST /api/users/register`

Request:

```json
{ "email": "runner@example.com", "password": "at-least-8-chars" }
```

Success: `201 Created`, `Location: api/users/me`, body:

```json
{ "id": "3f2c...guid", "email": "runner@example.com", "createdAtUtc": "2026-09-19T12:00:00Z" }
```

Registering does **not** log the user in and returns no token. The client must call login next.

Validation (all failures reported together):

| Code | Rule |
|---|---|
| `InvalidEmail` | Empty after trimming, longer than 320 chars, or no `@` |
| `PasswordTooShort` | Fewer than 8 characters |

Errors: `400` validation, `409` email already registered.

### `POST /api/users/login`

Request:

```json
{ "email": "runner@example.com", "password": "at-least-8-chars" }
```

Success: `200 OK`:

```json
{ "accessToken": "eyJhbGciOi...", "expiresAtUtc": "2026-09-19T12:15:00Z" }
```

Errors:

- `401` for unknown email **and** wrong password. Same status and same message (`Invalid email or password.`) for both, so callers cannot probe which emails exist.
- `429` after 5 attempts per minute from one IP.

### `GET /api/users/me`

Requires bearer token. Returns `{ id, email, createdAtUtc }`. Useful for the client to confirm a stored token is still valid and to load the user profile.

## Token

Issued by `JwtAccessTokenIssuer` (`src/TrailCompanion.Infrastructure/Authentication/`).

| Property | Value |
|---|---|
| Format | JWT (JWS, compact) |
| Algorithm | HS256 (HMAC-SHA256, symmetric) |
| `sub` | `AppUser.Id` as a GUID string |
| `iss` | `Jwt:Issuer`, default `trailcompanion-api` |
| `aud` | `Jwt:Audience`, default `trailcompanion-api` |
| `exp` | now + `Jwt:AccessTokenLifetimeMinutes`, default 15 |
| Other claims | None. No roles, no email, no personal data. Never add secrets or personal data. |

Validation (`AuthenticationConfiguration` in Api): signature, expiry, issuer, audience. Only HS256 is accepted. Clock skew tolerance is 30 seconds. `MapInboundClaims = false`, so the claim stays named `sub` instead of being remapped to a long .NET claim URI.

There is no refresh token, no token revocation and no logout endpoint. A token stays valid until it expires. Logging out is a client-side action (drop the token).

## Configuration

`Jwt` section (`JwtOptions`, bound in `Infrastructure/.../AuthenticationConfiguration.cs`):

| Key | Default | Notes |
|---|---|---|
| `Jwt:Issuer` | `trailcompanion-api` | In `appsettings.json` |
| `Jwt:Audience` | `trailcompanion-api` | In `appsettings.json` |
| `Jwt:AccessTokenLifetimeMinutes` | `15` | In `appsettings.json` |
| `Jwt:Key` | empty | **Secret. Never commit.** Minimum 32 bytes (UTF-8) |

The API **refuses to start** if `Jwt:Key` is under 32 bytes (`ValidateOnStart`). Set it once per machine with user-secrets:

```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/TrailCompanion.Api
```

In deployed environments, supply it as the environment variable `Jwt__Key` (or your secret store). Changing the key invalidates every issued token.

`Cors:AllowedOrigins` is a string array. Development allows `http://localhost:5173`. Production must list the real frontend origin(s); the base `appsettings.json` default is empty, meaning no browser origin is allowed.

## Password handling

`PasswordHasher` (`Infrastructure/Authentication/`):

- PBKDF2-HMAC-SHA256, 600,000 iterations, 16-byte random salt, 32-byte hash.
- Stored in `AppUsers.PasswordHash` as `iterations.salt.hash` (base64 salt and hash). Iteration count is stored per row, so it can be raised later and old hashes still verify.
- Comparison uses `CryptographicOperations.FixedTimeEquals`.
- `Verify(password, null)` still burns a full hash against a dummy value. Login therefore takes about the same time whether or not the email exists (no timing side channel). Keep this behavior when touching login.

## Email rules

- `UserEmail.Normalize` trims and lowercases. It is applied on register **and** login.
- Uniqueness is enforced by a unique index on `AppUsers.Email`, not by a pre-check. `Repository.SaveChangesAsync` translates the violation into `ConflictException` via `UniqueConstraintConflicts`, giving a `409`.
- Note: register returns `409` for an existing email, so registration does reveal whether an email is taken. Login does not.

## Request pipeline

Order is load-bearing (`Program.cs`):

1. `CorrelationIdMiddleware`: assigns or accepts `X-Correlation-Id`.
2. `ExceptionHandlingMiddleware`: turns exceptions into `ProblemDetails`.
3. CORS (`SpaCors` policy).
4. `UseRateLimiter()`.
5. `UseAuthentication()`.
6. `UseAuthorization()`.
7. Endpoints.

Per-route rules are set on each endpoint class individually: `RequireAuthorization()` for protected, `AllowAnonymous()` for public. No route group inherits either, and no fallback authorization policy is set, so a new endpoint with neither is open to everyone. Always declare one, and verify a new endpoint returns 401 without a token.

## Who is the caller? (`ICurrentUser`)

- `ICurrentUser.AppUserId` (Application contract, implemented in Api `CurrentUser`) reads the `sub` claim from `HttpContext.User`. Returns `null` if unauthenticated or `sub` is not a GUID.
- The Application layer never touches `HttpContext`.
- Endpoints pass `AppUserId` into the command/query. **The user id must come from the token, never from route or body.**

## Ownership and authorization

There are no roles. Authorization is ownership only:

- Owned entities (races, nutrition items) carry `AppUserId`; segments and items are owned through the race chain.
- Handlers take `appUserId` and filter on it. Ownership is enforced in the handler, not the endpoint.
- Another user's record returns `404`, not `403`. A 403 would confirm the record exists.

## Error responses

All errors from handlers use `application/problem+json` (`ProblemDetails`) with a `correlationId` extension.

| Exception | Status |
|---|---|
| `ValidationFailedException`, `DomainException` | 400 |
| `UnauthorizedException` | 401 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| anything else | 500 |

Shape:

```json
{
  "status": 400,
  "title": "Invalid request",
  "detail": "The registration request is invalid.",
  "instance": "/api/users/register",
  "correlationId": "9b1d...",
  "validationErrors": [
    { "code": "PasswordTooShort", "message": "Password must be at least 8 characters." }
  ]
}
```

`validationErrors` appears on 400 (validation) and 409 (single entry). In Development, `detail` and `stackTrace` are also filled for unexpected 500s.

**Gotcha: two responses are not `ProblemDetails`.** A `401` from a missing, expired or invalid token is produced by the JwtBearer challenge, before any handler runs. It has an empty body and a `WWW-Authenticate: Bearer` header (with `error="invalid_token"` and a description when a token was present but bad). A `429` from the rate limiter also has an empty body. The login `401` for bad credentials, by contrast, is a `ProblemDetails`. Clients must not assume a JSON body on 401 or 429.

## Rate limiting

- Applies to `POST /api/users/login` only.
- Fixed window: 5 requests per 1 minute, partitioned by `RemoteIpAddress`. Exceeding returns `429`.
- Register is **not** rate limited.
- No `Retry-After` header is configured.
- No forwarded-headers middleware is configured. Behind a reverse proxy or load balancer, `RemoteIpAddress` is the proxy, so all users share one bucket. Add `UseForwardedHeaders` if deploying behind one.

## HTTPS

No HTTPS redirection or HSTS is configured in code. Terminate TLS at the host or proxy. Tokens must only travel over HTTPS outside local dev.

## Observability

Every request gets a correlation id. Inbound `X-Correlation-Id` is accepted if it is 1 to 64 characters of letters, digits, `-`, `_`, `.` (and is not the literal `unassigned`). Otherwise a fresh GUID (no dashes) is generated. It is echoed on the response header and included in `ProblemDetails.correlationId`. It appears in log scopes.

## Where the code lives

| Concern | File |
|---|---|
| Route constants | `Api/Endpoints/ApiEndpoints.cs` |
| Register / login / me endpoints | `Api/Endpoints/Users/{RegisterUser,LoginUser,GetMyUser}Endpoint.cs` |
| JWT validation setup | `Api/Common/Configuration/DependencyInjection/AuthenticationConfiguration.cs` |
| CORS | `Api/Common/Configuration/DependencyInjection/CorsConfiguration.cs` |
| Rate limiting | `Api/Common/Configuration/DependencyInjection/RateLimitingConfiguration.cs` |
| `ICurrentUser` impl | `Api/Common/Services/CurrentUser.cs` |
| Error mapping | `Api/Common/Middleware/ExceptionHandlingMiddleware.cs` |
| Register / login handlers | `Application/Modules/Users/Commands/{RegisterUser,LoginUser}Command.cs` |
| Contracts | `Application/Common/Contracts/{IPasswordHasher,IAccessTokenIssuer}.cs` |
| `JwtOptions` | `Application/Common/Configuration/JwtOptions.cs` |
| Token issuing | `Infrastructure/Authentication/JwtAccessTokenIssuer.cs` |
| Password hashing | `Infrastructure/Authentication/PasswordHasher.cs` |
| User table config | `Infrastructure/Persistence/Configuration/Users/AppUserConfiguration.cs` |

## Adding a protected endpoint

1. Implement `IEndpoint` (a class that does not is silently skipped and the route 404s).
2. Add a route constant to `ApiEndpoints`.
3. Call `.RequireAuthorization()` on the route.
4. Inject `ICurrentUser`; return `TypedResults.Unauthorized()` if `AppUserId` is null; pass the id to the command or query.
5. In the handler, filter by `appUserId`; throw `NotFoundException` on a miss.
6. Test that it returns 401 with no token.

## Known limitations

- No refresh tokens: users are logged out every 15 minutes.
- No revocation or logout endpoint. A leaked token is valid until `exp`.
- No password reset, email verification, or account lockout (only the per-IP login rate limit).
- No roles or scopes.
- Register is unthrottled and reveals whether an email is taken (409).

---

# Part 2: Frontend

## Flow

```
Register (once)   POST /api/users/register  -> 201 { id, email, createdAtUtc }
Login             POST /api/users/login     -> 200 { accessToken, expiresAtUtc }
Every request     Authorization: Bearer <accessToken>
Token expired     any protected call -> 401 (empty body) -> send user to login
```

## Calling the API

- Local API: `http://localhost:5170`. Swagger UI at `/swagger` (Development only).
- JSON bodies, `Content-Type: application/json`. JSON property names are camelCase.
- Protected requests need the header `Authorization: Bearer <accessToken>`.
- Dates are UTC ISO 8601 strings.

### CORS

The API allows only origins listed in `Cors:AllowedOrigins`. In Development that is `http://localhost:5173` (Vite default). Serve the frontend from another origin or port and browser calls fail on CORS; add the origin to `appsettings.Development.json` (local) or config (deployed).

- Allowed methods: `GET, POST, PUT, PATCH, DELETE`.
- Allowed request headers: `Authorization`, `Content-Type`, `X-Correlation-Id`. Any other custom header fails preflight.
- Exposed response header: `X-Correlation-Id`.
- No credentials or cookies are used, so do not set `credentials: 'include'`. Auth is the header only.

## Token handling

- Store `accessToken` and `expiresAtUtc` after login.
- Lifetime is 15 minutes. There is no refresh endpoint. Either log the user out at `expiresAtUtc` or handle 401 and redirect to login.
- Compare `expiresAtUtc` with a small margin (the server allows 30 s skew, do not rely on it) and treat the session as ended slightly before expiry.
- Storage tradeoff: `localStorage` survives reloads but is readable by any script on the page (XSS). In-memory is safer but lost on reload, and with no refresh token the user must log in again. Pick deliberately. Never put the token in a URL.
- The token is a JWT; the client may decode the payload for `sub` or `exp` but must not trust or rely on it for anything the API does not verify. There is no email or role in it. Call `GET /api/users/me` for profile data.
- Logout: delete the stored token. Nothing to call on the server.

## Handling responses

| Status | Meaning | Body | Client action |
|---|---|---|---|
| 200 / 201 | Success | JSON | Proceed |
| 400 | Validation failed | ProblemDetails, `validationErrors[]` | Show per-field messages by `code` |
| 401 on login | Bad email or password | ProblemDetails | Show a generic "Invalid email or password" |
| 401 on other routes | Missing, expired or invalid token | **Empty** | Clear token, redirect to login |
| 404 | Not found, **or owned by another user** | ProblemDetails | Treat as missing |
| 409 | Email already registered (register) | ProblemDetails with `validationErrors` | Tell user, offer login |
| 429 | Login rate limit hit | **Empty** | Show "try again in a minute" |
| 500 | Server error | ProblemDetails | Show generic error, log `correlationId` |

Because 401 and 429 can have empty bodies, check `response.status` first and parse JSON only when `Content-Type` is `application/problem+json`.

Validation error codes to map to UI messages:

- `InvalidEmail`
- `PasswordTooShort` (min 8 characters)

Mirror these rules client-side for fast feedback, but the server is the source of truth.

## Correlation id

Optionally send `X-Correlation-Id` (letters, digits, `-`, `_`, `.`, max 64 chars) to trace a request across client and server logs. The server returns the id on every response in the `X-Correlation-Id` header and in `ProblemDetails.correlationId`. Include it in bug reports and error toasts.

## Minimal client sketch

```ts
const API = "http://localhost:5170";

async function login(email: string, password: string) {
  const res = await fetch(`${API}/api/users/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (res.status === 401) throw new Error("Invalid email or password");
  if (res.status === 429) throw new Error("Too many attempts, wait a minute");
  if (!res.ok) throw new Error("Login failed");
  return (await res.json()) as { accessToken: string; expiresAtUtc: string };
}

async function api(path: string, token: string, init: RequestInit = {}) {
  const res = await fetch(`${API}${path}`, {
    ...init,
    headers: {
      ...init.headers,
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });
  if (res.status === 401) {
    // clear stored token, redirect to /login
  }
  return res;
}
```

## Frontend checklist

- Login screen calls login; on register success, immediately call login (register returns no token).
- Attach the bearer header on every request except register and login.
- Central 401 handler that clears the token and redirects to login.
- Log the user out when `expiresAtUtc` passes.
- Disable the login button briefly after a 429.
- Show `validationErrors` from 400 and 409 responses.
- Serve the frontend from an origin present in `Cors:AllowedOrigins`.
- Never send user ids in routes or bodies to select "whose" data. The API derives the user from the token.
