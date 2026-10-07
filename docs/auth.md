# Authentication

How auth works in TrailCompanion API. Part 1 is for anyone working on the API. Part 2 is for anyone building a client (the frontend repo).

## TL;DR

- Self-issued JWT bearer tokens. No ASP.NET Core Identity, no OAuth provider.
- Register with username + password, log in to get an access token, send it as `Authorization: Bearer <token>`.
- Access token lives 15 minutes. Login also sets a 30-day refresh token in an `HttpOnly` cookie; `POST /api/users/refresh` swaps it for a new access token and a rotated cookie.
- The token carries one claim that matters: `sub` = the user's `AppUser.Id` (GUID).
- Every endpoint except register, login, refresh and logout requires a valid token. This is enforced by a fallback policy, so a new endpoint without `AllowAnonymous()` is closed. `EndpointAuthorizationTests` checks every route.

---

# Part 1: Backend

## Endpoints

Base path is `api`. Route constants live in `src/TrailCompanion.Api/Endpoints/ApiEndpoints.cs`.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/users/register` | Anonymous, rate limited | Create an account |
| POST | `/api/users/login` | Anonymous, rate limited, Origin-checked | Exchange credentials for an access token and a refresh cookie |
| POST | `/api/users/refresh` | Anonymous (cookie is the credential), rate limited, Origin-checked | Rotate the refresh cookie, return a new access token |
| POST | `/api/users/logout` | Anonymous, Origin-checked | Revoke the refresh cookie and clear it |
| GET | `/api/users/me` | Bearer required | Return the caller's account |
| PUT | `/api/users/me` | Bearer required | Set or clear first and last name |
| DELETE | `/api/users/me` | Bearer required, rate limited | Permanently delete the account (password required) |
| POST | `/api/users/password` | Bearer required, rate limited | Change password (current password required) |
| GET, POST | `/api/races` | Bearer required | List / create races |
| GET, DELETE | `/api/races/{id}` | Bearer required | Read / delete a race |
| POST | `/api/races/{raceId}/segments` | Bearer required | Create segment |
| PUT, DELETE | `/api/races/{raceId}/segments/{id}` | Bearer required | Update / delete segment |
| POST | `/api/races/{raceId}/segments/{segmentId}/items` | Bearer required | Attach a library item to a segment |
| DELETE | `/api/races/{raceId}/segments/{segmentId}/items/{id}` | Bearer required | Delete item |
| POST | `/api/races/{raceId}/aid-stations` | Bearer required | Add an aid station before a segment |
| PUT, DELETE | `/api/races/{raceId}/aid-stations/{id}` | Bearer required | Update / delete aid station |
| PUT | `/api/races/{raceId}/aid-stations/{id}/move` | Bearer required | Move aid station to another segment (swaps if occupied) |
| POST | `/api/races/{raceId}/aid-stations/{aidStationId}/items` | Bearer required | Attach a library item to an aid station |
| PUT | `/api/races/{raceId}/aid-stations/{aidStationId}/items/order` | Bearer required | Reorder aid station items |
| DELETE | `/api/races/{raceId}/aid-stations/{aidStationId}/items/{id}` | Bearer required | Delete aid station item |
| GET, POST | `/api/nutrition-items` | Bearer required | List / create nutrition library item |
| PUT, DELETE | `/api/nutrition-items/{id}` | Bearer required | Update / delete nutrition library item |

All `{id}` style route params are constrained to GUIDs.

### `POST /api/users/register`

Request:

```json
{ "username": "runner", "password": "at-least-8-chars" }
```

Success: `201 Created`, `Location: api/users/me`, body:

```json
{ "id": "3f2c...guid", "username": "runner", "firstName": null, "lastName": null, "createdAtUtc": "2026-09-19T12:00:00Z" }
```

Registering does **not** log the user in and returns no token. The client must call login next.

Validation (all failures reported together):

| Code | Rule |
|---|---|
| `InvalidUsername` | After trimming and lowercasing, not 3 to 32 characters from a-z, 0-9, _, . or - |
| `PasswordTooShort` | Fewer than 8 characters |
| `PasswordTooLong` | More than 128 characters |
| `PasswordBreached` | In the HIBP corpus (only with `FeatureManagement:BreachedPasswordCheck` on) |

Password rules live in one shared class, `Application/Modules/Users/PasswordRules.cs`, used by register and change password.

Errors: `400` validation, `409` username already taken (`DuplicateUsername`, "That username is taken."), `429` rate limited.

Rate limited, 10 per minute per client. `400 PasswordTooLong` above 128 characters. With `FeatureManagement:BreachedPasswordCheck` on, `400 PasswordBreached` when the password is in the HIBP corpus; the check fails open after 3 seconds. A taken username returns `409` with "That username is taken." This is deliberate: usernames are public identifiers (audit L3, accepted).

### `POST /api/users/login`

Request:

```json
{ "username": "runner", "password": "at-least-8-chars" }
```

Success: `200 OK`:

```json
{ "accessToken": "eyJhbGciOi...", "expiresAtUtc": "2026-09-19T12:15:00Z" }
```

Errors:

- `401` for unknown username **and** wrong password. Same status and same message (`Invalid username or password.`) for both, so callers cannot probe which usernames exist. Login does not check the username format.
- `429` after 5 attempts per minute from one client.
- `401` "Too many failed attempts. Try again in 15 minutes." After 10 failed attempts for one username within 15 minutes, that username gets this `401` until the window ends, whatever the password. In-memory, per instance, cleared on restart.
- `403` when the `Origin` header is not allowed (see Cross-site requests).

On success the response also carries `Set-Cookie: refresh_token=...; HttpOnly; Secure; SameSite=None; Path=/api/users; Expires=...`.

### `POST /api/users/refresh`

No body, no bearer. The `refresh_token` cookie is the credential. Success: `200` with the same body as login and a new cookie. The presented token is marked replaced and stops working immediately (rotation). Rotation (`IRefreshTokensRepository.RotateAsync`) revokes the old token with a conditional update and inserts the new one in one database transaction, so concurrent refreshes with one cookie succeed exactly once; a loser counts as reuse and revokes every token for the user. A session ends 90 days after login however often it rotates. Sessions that existed before the `SessionStartedAtUtc` migration were backfilled from `CreatedAtUtc`, so they get up to 90 days from their latest rotation (one-time).

`401` (ProblemDetails, no cookie set) when the cookie is absent, unknown, expired or revoked. Presenting a token that was already rotated or revoked is treated as theft: **every** refresh token for that user is revoked, so the user must log in again on every device. `429` after 30 requests per minute from one client. `403` for a foreign `Origin`.

### `POST /api/users/logout`

No body, no bearer (it must work after the access token expired). Revokes the presented refresh token if active and clears the cookie. Always `204`.

### `GET /api/users/me`

Requires bearer token. Returns `{ id, username, firstName, lastName, createdAtUtc }`; names are `null` when unset. Useful for the client to confirm a stored token is still valid and to load the user profile.

### `PUT /api/users/me`

Body `{ "firstName": "...", "lastName": "..." }`. Both are trimmed; empty or whitespace-only is stored as `null`, so sending `null` or `""` clears a name. Max 100 characters each (`FirstNameTooLong`, `LastNameTooLong`). The username cannot be changed here. Returns the updated `AppUserDto`.

### `POST /api/users/password`

Body `{ "currentPassword": "...", "newPassword": "..." }`. Rate limited, 5 per minute per client.

- `401` ProblemDetails when `currentPassword` is wrong. Clients must not treat this 401 as a session expiry.
- `400` with the register password codes (`PasswordTooShort`, `PasswordTooLong`, `PasswordBreached`), or `PasswordUnchanged` when the new password equals the current one.
- `204` on success. Every refresh token for the user **except the caller's current cookie** is revoked, so other devices must log in again. Access tokens issued before the change are rejected (`PasswordChangedAtUtc`, checked in `OnTokenValidated`). The caller's kept cookie refreshes into a valid token.

### `DELETE /api/users/me`

Body `{ "password": "..." }`. Rate limited, 5 per minute per client. `401` ProblemDetails on a wrong password. On success returns `204` and clears the refresh cookie.

Deletion is a **permanent hard delete**, with no grace period or undo: the user row and all their races, segments, items, aid stations, aid station items, nutrition items and refresh tokens are removed in one transaction. Access tokens of a deleted user are rejected with `401` (previously `404`).

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
| Other claims | None. No roles, no username, no personal data. Never add secrets or personal data. |

Validation (`AuthenticationConfiguration` in Api): signature, expiry, issuer, audience. Only HS256 is accepted. Clock skew tolerance is 30 seconds. `MapInboundClaims = false`, so the claim stays named `sub` instead of being remapped to a long .NET claim URI.

There is no access-token revocation list, but `OnTokenValidated` rejects a token issued before the user's `PasswordChangedAtUtc`, and a token whose user no longer exists gets `401` (not `404`). Refresh tokens can be revoked.

## Refresh tokens

Issued by `RefreshTokenIssuer` (`Infrastructure/Authentication/`).

| Property | Value |
|---|---|
| Value | 32 random bytes from `RandomNumberGenerator`, base64url. Opaque, not a JWT |
| Storage | `RefreshTokens` table stores only the SHA-256 hash (`TokenHash`, unique index). The raw value is never stored or logged |
| Lifetime | `Jwt:RefreshTokenLifetimeDays`, default 30 per token; session capped at 90 days from login (`SessionStartedAtUtc`). |
| Rotation | Every refresh revokes the old row, sets `ReplacedByTokenId`, and issues a new one |
| Reuse detection | A revoked or replaced token presented again revokes every token for that user |
| Cookie | `refresh_token`, `HttpOnly`, `Secure`, `SameSite=None`, `Path=/api/users`, explicit `Expires` |

`SameSite=None` is required because the SPA and API are on different sites in deployment. It requires `Secure`, so the cookie only travels over HTTPS; browsers exempt `http://localhost`, so local development works. Expired and revoked rows are not cleaned up yet (no job scheduler).

Cross-site requests: login, refresh and logout reject any `Origin` header not in `Cors:AllowedOrigins` with `403`. Requests with no `Origin` (non-browser clients) pass. The Development-only Swagger UI runs on the API origin (`http://localhost:5170`), which is not in `Cors:AllowedOrigins`, so login, refresh and logout from Swagger UI return `403`; use the SPA or a client that sends no `Origin` (curl).

## Configuration

`Jwt` section (`JwtOptions`, bound in `Infrastructure/.../AuthenticationConfiguration.cs`):

| Key | Default | Notes |
|---|---|---|
| `Jwt:Issuer` | `trailcompanion-api` | In `appsettings.json` |
| `Jwt:Audience` | `trailcompanion-api` | In `appsettings.json` |
| `Jwt:AccessTokenLifetimeMinutes` | `15` | In `appsettings.json` |
| `Jwt:RefreshTokenLifetimeDays` | `30` | `JwtOptions` default |
| `Jwt:Key` | empty | **Secret. Never commit.** Minimum 32 bytes (UTF-8) |

The API **refuses to start** if `Jwt:Key` is under 32 bytes (`ValidateOnStart`). Set it once per machine with user-secrets:

```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/TrailCompanion.Api
```

In deployed environments, supply it as the environment variable `Jwt__Key` (or your secret store). Changing the key invalidates every issued token.

`Cors:AllowedOrigins` is a string array. Development allows `http://localhost:5173`. Production must list the real frontend origin(s), exact scheme plus host with no trailing slash; the base `appsettings.json` default is empty, meaning no browser origin is allowed. The policy calls `AllowCredentials()` so the refresh cookie is sent, which is only safe because origins are an explicit list, never a wildcard.

`DATABASE_URL_MIGRATIONS` (`ConnectionStrings:MigrationsConnection`) is the owner role, a direct URL, used for migrations only. The app itself connects with `DATABASE_URL` as a non-owner role.

### Registration feature flag

`FeatureManagement:RegistrationEnabled` (via `Microsoft.FeatureManagement`) controls whether `POST /api/users/register` creates accounts. `appsettings.json` defaults it to `true`. Do not remove that default: a feature with no definition counts as disabled.

| Env var | Config key | Values |
|---|---|---|
| `FEATURE_REGISTRATION_ENABLED` | `FeatureManagement:RegistrationEnabled` | Unset or blank means enabled. `true` (any case) means enabled. Anything else, including typos, means disabled. |

`FEATURE_BREACHED_PASSWORD_CHECK` (`FeatureManagement:BreachedPasswordCheck`) switches the breached-password check on register on or off.

The value is read when the request arrives, from configuration loaded at startup. To change it, set the variable and restart the service. The web app has its own variable with the same name, set at build time. The two are not linked: the deploy must keep them the same. The API is the real gate. If the web app says open but the API says closed, users see "Account creation is currently closed" when they submit.

## Password handling

`PasswordHasher` (`Infrastructure/Authentication/`):

- PBKDF2-HMAC-SHA256, 600,000 iterations, 16-byte random salt, 32-byte hash.
- Stored in `AppUsers.PasswordHash` as `iterations.salt.hash` (base64 salt and hash). Iteration count is stored per row, so it can be raised later and old hashes still verify.
- Comparison uses `CryptographicOperations.FixedTimeEquals`.
- `Verify(password, null)` still burns a full hash against a dummy value. Login therefore takes about the same time whether or not the username exists (no timing side channel). Keep this behavior when touching login.

## Username rules

- `UserName.Normalize` trims and lowercases. It is applied on register **and** login, so usernames are case-insensitive.
- Register also requires `UserName.IsValid`: 3 to 32 characters from a-z, 0-9, `_`, `.` and `-`. Login skips this check, so a stored username outside the rules (the account migrated from an email) still logs in.
- Uniqueness is enforced by a unique index on `AppUsers.Username` (`IX_AppUsers_Username`), not by a pre-check. `Repository.SaveChangesAsync` translates the violation into `ConflictException` via `UniqueConstraintConflicts`, giving a `409`.
- Note: register returns `409` for an existing username, so registration does reveal whether a username is taken. Login does not. Accepted (audit L3): usernames are public identifiers.

## Request pipeline

Order is load-bearing (`Program.cs`):

1. `CorrelationIdMiddleware`: assigns or accepts `X-Correlation-Id`.
2. `ExceptionHandlingMiddleware`: turns exceptions into `ProblemDetails`.
3. CORS (`SpaCors` policy).
4. `UseRateLimiter()`.
5. `UseAuthentication()`.
6. `UseAuthorization()`.
7. Endpoints.

`ForwardedHeaders` runs before all of these.

Per-route rules are set on each endpoint class individually: `RequireAuthorization()` for protected, `AllowAnonymous()` for public. No route group inherits either, but a fallback policy requires authentication, so a new endpoint with neither is closed. Always declare one, and verify a new endpoint returns 401 without a token. `EndpointAuthorizationTests` checks every route.

## Who is the caller? (`ICurrentUser`)

- `ICurrentUser.AppUserId` (Application contract, implemented in Api `CurrentUser`) reads the `sub` claim from `HttpContext.User`. Returns `null` if unauthenticated or `sub` is not a GUID.
- The Application layer never touches `HttpContext`.
- Endpoints pass `AppUserId` into the command/query. **The user id must come from the token, never from route or body.**

## Ownership and authorization

There are no roles. Authorization is ownership only:

- Owned entities (races, nutrition items) carry `AppUserId`; segments, items, aid stations and aid station items are owned through the race chain.
- Handlers take `appUserId` and filter on it. Ownership is enforced in the handler, not the endpoint.
- Another user's record returns `404`, not `403`. A 403 would confirm the record exists.

## Error responses

All errors from handlers use `application/problem+json` (`ProblemDetails`) with a `correlationId` extension.

| Exception | Status |
|---|---|
| `ValidationFailedException`, `DomainException` | 400 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
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

Fixed window of 1 minute, partitioned by client address (see below). Exceeding returns `429`.

| Policy | Routes | Limit |
|---|---|---|
| `register` | `POST /api/users/register` | 10 |
| `login` | `POST /api/users/login` | 5 |
| `refresh` | `POST /api/users/refresh` | 30 |
| `account` | `POST /api/users/password`, `DELETE /api/users/me` | 5 |

- No `Retry-After` header is configured.

## Rate limiting and client address

Partitioned per client: the rightmost `X-Forwarded-For` hop (`ForwardLimit = 1`, `KnownIPNetworks` and `KnownProxies` cleared). This is safe only because the container is reachable solely through Render's proxy; if that changes, set `KnownIPNetworks` to the proxy range. IPv6 is grouped by /64. Password hashing (register, login, change password, delete account) is additionally capped at 2 concurrent requests, queue 10, else `429`.

## HTTPS

No HTTPS redirection or HSTS is configured in code. Terminate TLS at the host or proxy. Tokens must only travel over HTTPS outside local dev.

## Observability

Every request gets a correlation id. Inbound `X-Correlation-Id` is accepted if it is 1 to 64 characters of letters, digits, `-`, `_`, `.` (and is not the literal `unassigned`). Otherwise a fresh GUID (no dashes) is generated. It is echoed on the response header and included in `ProblemDetails.correlationId`. It appears in log scopes.

## Where the code lives

| Concern | File |
|---|---|
| Route constants | `Api/Endpoints/ApiEndpoints.cs` |
| Account endpoints | `Api/Endpoints/Users/{RegisterUser,LoginUser,RefreshToken,Logout,GetMyUser,UpdateMyUser,DeleteMyUser,ChangePassword}Endpoint.cs` |
| Refresh cookie read/write | `Api/Common/Services/RefreshTokenCookie.cs` |
| JWT validation setup | `Api/Common/Configuration/DependencyInjection/AuthenticationConfiguration.cs` |
| CORS | `Api/Common/Configuration/DependencyInjection/CorsConfiguration.cs` |
| Rate limiting | `Api/Common/Configuration/DependencyInjection/RateLimitingConfiguration.cs` |
| Per-account quotas | `Application/Modules/Users/Commands/AccountLimitRules.cs` |
| `ICurrentUser` impl | `Api/Common/Services/CurrentUser.cs` |
| Error mapping | `Api/Common/Middleware/ExceptionHandlingMiddleware.cs` |
| Account handlers | `Application/Modules/Users/Commands/{RegisterUser,LoginUser,RefreshAccessToken,Logout,UpdateProfile,ChangePassword,DeleteAccount}Command.cs` |
| Password rules | `Application/Modules/Users/PasswordRules.cs` |
| Contracts | `Application/Common/Contracts/{IPasswordHasher,IAccessTokenIssuer,IRefreshTokenIssuer}.cs` |
| `JwtOptions` | `Application/Common/Configuration/JwtOptions.cs` |
| Token issuing | `Infrastructure/Authentication/{JwtAccessTokenIssuer,RefreshTokenIssuer}.cs` |
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

- A leaked access token is valid until `exp` (at most 15 minutes), even after logout. Password change and account deletion do reject older tokens.
- Expired and revoked refresh-token rows are never deleted.
- No email is stored. There is no password reset: a forgotten password is fixed in the database. The login throttle is per username, in-memory and per instance; no other account lockout. No in-app MFA.
- Usernames cannot be changed.
- No roles or scopes.
- Register reveals whether a username is taken (409). Accepted.

---

# Part 2: Frontend

## Flow

```
Register (once)   POST /api/users/register  -> 201 { id, username, firstName, lastName, createdAtUtc }
Login             POST /api/users/login     -> 200 { accessToken, expiresAtUtc } + refresh cookie
Every request     Authorization: Bearer <accessToken>
Before expiry     POST /api/users/refresh   -> 200 { accessToken, expiresAtUtc } + rotated cookie
Token expired     any protected call -> 401 (empty body) -> refresh once, retry; refresh 401 -> send user to login
Logout            POST /api/users/logout    -> 204, cookie cleared
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
- Credentials are allowed. Every request must use `credentials: 'include'`, or the browser neither stores nor sends the refresh cookie.

## Token handling

- Store `accessToken` and `expiresAtUtc` after login.
- Lifetime is 15 minutes. Call `POST /api/users/refresh` shortly before `expiresAtUtc`, and on a 401 try one refresh and retry. Share one in-flight refresh between concurrent 401s: presenting a rotated cookie twice revokes the whole session.
- The refresh token lives only in the `HttpOnly` cookie. JavaScript cannot read it and must never try to store it.
- Compare `expiresAtUtc` with a small margin (the server allows 30 s skew, do not rely on it) and treat the session as ended slightly before expiry.
- Storage tradeoff: `localStorage` survives reloads but is readable by any script on the page (XSS). In-memory is safer; with the refresh cookie a reload just costs one refresh call. Never put the token in a URL.
- The token is a JWT; the client may decode the payload for `sub` or `exp` but must not trust or rely on it for anything the API does not verify. There is no username or role in it. Call `GET /api/users/me` for profile data.
- Logout: call `POST /api/users/logout`, then delete the stored access token.

## Handling responses

| Status | Meaning | Body | Client action |
|---|---|---|---|
| 200 / 201 | Success | JSON | Proceed |
| 400 | Validation failed | ProblemDetails, `validationErrors[]` | Show per-field messages by `code` |
| 401 on login | Bad username or password | ProblemDetails | Show a generic "Invalid username or password" |
| 401 on other routes | Missing, expired or invalid token | **Empty** | Refresh once and retry; if refresh fails, clear token and redirect to login |
| 401 on password change or delete account | Wrong current password | ProblemDetails | Show inline error, do not log out |
| 403 on register | Account creation is closed (`FEATURE_REGISTRATION_ENABLED`) | ProblemDetails | Show "Account creation is currently closed", offer login |
| 404 | Not found, **or owned by another user** | ProblemDetails | Treat as missing |
| 409 | Username already taken (register) | ProblemDetails with `validationErrors` | Tell user, offer login |
| 403 on login, refresh, logout | Foreign `Origin` | ProblemDetails | Should not happen from the SPA; check `Cors:AllowedOrigins` |
| 429 | Rate limit hit | **Empty** | Show "try again in a minute" |
| 500 | Server error | ProblemDetails | Show generic error, log `correlationId` |

Because 401 and 429 can have empty bodies, check `response.status` first and parse JSON only when `Content-Type` is `application/problem+json`.

Validation error codes to map to UI messages:

- `InvalidUsername`
- `PasswordTooShort` (min 8 characters)
- `PasswordTooLong` (max 128 characters)
- `PasswordBreached`
- `LimitReached` (per-account quota on the create endpoints)
- `PasswordUnchanged`
- `FirstNameTooLong`, `LastNameTooLong` (max 100 characters)

Mirror these rules client-side for fast feedback, but the server is the source of truth.

## Correlation id

Optionally send `X-Correlation-Id` (letters, digits, `-`, `_`, `.`, max 64 chars) to trace a request across client and server logs. The server returns the id on every response in the `X-Correlation-Id` header and in `ProblemDetails.correlationId`. Include it in bug reports and error toasts.

## Minimal client sketch

```ts
const API = "http://localhost:5170";

async function login(username: string, password: string) {
  const res = await fetch(`${API}/api/users/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });
  if (res.status === 401) throw new Error("Invalid username or password");
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
- Send every request with `credentials: 'include'`.
- Central 401 handler that refreshes once, retries, and only then clears the token and redirects to login.
- Refresh shortly before `expiresAtUtc`.
- Disable the login button briefly after a 429.
- Show `validationErrors` from 400 and 409 responses.
- Serve the frontend from an origin present in `Cors:AllowedOrigins`.
- Never send user ids in routes or bodies to select "whose" data. The API derives the user from the token.
