# Rolling Your Own Auth in .NET (Basic)

Goal: register/login with hashed passwords, issue a JWT, protect endpoints with it. No ASP.NET Core Identity. One NuGet package.

```
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

That package only *validates* tokens (signature, expiry, issuer, audience). Users, passwords, login and token issuing are all your code.

---

## 1. Passwords: do NOT use plain SHA

> **Warning:** `SHA256(password)` (even salted) is not safe for passwords. SHA is built to be fast; a GPU can try billions of guesses per second against a leaked table. Use a deliberately slow key-derivation function.

The stdlib answer is **PBKDF2 with HMAC-SHA256**: it *is* SHA, just run 600,000 times with a random salt (OWASP's current recommendation). It is built into .NET 6+, so there's no package.

```csharp
using System.Security.Cryptography;

public static class PasswordHasher
{
    const int Iterations = 600_000, SaltSize = 16, HashSize = 32;
    static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    // Stored as "iterations.salt.hash" so iterations can be raised later
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected); // constant-time compare
    }
}
```

Stronger alternative: Argon2id (`Konscious.Security.Cryptography.Argon2` package). PBKDF2 is fine for a basic app.

## 2. Users table

See `users` and `races.user_id` in `schema.md`. Store `email` lowercased and `password_hash` as `"iterations.salt.hash"`.

## 3. Program.cs

`db` calls below are pseudocode; use EF Core, Dapper, whatever you like.

```csharp
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 32+ random bytes, from env var / user-secrets, never in source control:
//   export Jwt__Key=$(openssl rand -base64 48)
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // keep "sub" as "sub"
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "race-planner",
            ValidAudience = "race-planner",
            IssuerSigningKey = jwtKey,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256], // reject any other alg
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// Brute-force protection: 5 login attempts per minute per IP (built into .NET 7+)
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

var dummyHash = PasswordHasher.Hash("dummy");

app.MapPost("/register", async (Credentials c) =>
{
    if (string.IsNullOrWhiteSpace(c.Email) || c.Password is not { Length: >= 12 })
        return Results.BadRequest("Email required, password min 12 chars.");

    var email = c.Email.Trim().ToLowerInvariant();
    if (await db.UserExists(email)) return Results.Conflict();

    await db.InsertUser(email, PasswordHasher.Hash(c.Password));
    return Results.Ok();
});

app.MapPost("/login", async (Credentials c) =>
{
    var user = await db.FindUserByEmail(c.Email.Trim().ToLowerInvariant());

    // Always run the hash, so response time doesn't reveal whether the email exists
    var ok = PasswordHasher.Verify(c.Password, user?.PasswordHash ?? dummyHash) && user is not null;
    if (!ok) return Results.Unauthorized(); // same response for "no user" and "wrong password"

    var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())]),
        Issuer = "race-planner",
        Audience = "race-planner",
        Expires = DateTime.UtcNow.AddMinutes(15),
        SigningCredentials = new SigningCredentials(jwtKey, SecurityAlgorithms.HmacSha256),
    });
    return Results.Ok(new { token });
}).RequireRateLimiting("login");

// Any protected endpoint: RequireAuthorization() + read the user id from "sub"
app.MapGet("/races", async (ClaimsPrincipal u) =>
{
    var userId = int.Parse(u.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    return await db.RacesForUser(userId); // always filter by the token's user id
}).RequireAuthorization();

app.Run();

record Credentials(string Email, string Password);
```

Client sends the token on every call:

```
Authorization: Bearer <token>
```

## 4. How it fits together

1. **Register:** validate input, hash the password with PBKDF2, store email + hash.
2. **Login:** look up the user, verify the password in constant time, sign a JWT (HMAC-SHA256) with `sub = user id`, 15-minute expiry.
3. **Request:** JwtBearer middleware checks signature, expiry, issuer and audience, then fills `ClaimsPrincipal`. Invalid or missing token gets 401.
4. **Authorisation:** your code filters data by the `sub` claim. A valid token only proves *who* the caller is; checking that they own race 42 is still your job.

## 5. Checklist

- HTTPS only. A token over plain HTTP is a stolen token.
- JWT key: at least 32 random bytes, from config/env, rotated if leaked (rotating logs everyone out).
- Don't put secrets or personal data in the JWT. The payload is base64, not encrypted.
- Short expiry (15 min). No server-side revocation exists for a plain JWT; expiry is the revocation.
- Browser storage: a JS-readable `localStorage` token is exposed to any XSS. Safer: keep it in memory, or send it as an `HttpOnly; Secure; SameSite=Strict` cookie.

## 6. Skipped (add when needed)

- **Refresh tokens:** add when 15-minute re-logins annoy users. Store a random opaque token (hashed) in a `refresh_tokens` table, with expiry, and rotate it on each use.
- **Logout / revocation:** comes with refresh tokens (delete the row).
- **Email verification, password reset:** single-use random tokens with expiry, stored hashed, sent by email.
- **Roles:** add a `role` claim and `RequireAuthorization(p => p.RequireRole("admin"))` when a second kind of user exists.

---

Verified: this code (with an in-memory dictionary instead of `db`) was built and run on .NET 8. Register/duplicate/short password gave 200/409/400. Wrong password and unknown email both gave 401. A valid token reached `/me`. Missing or tampered tokens gave 401. The 6th login in a minute gave 429.
