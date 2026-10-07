using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Http.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class RefreshTokenFlowTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public RefreshTokenFlowTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_SetsAScopedHttpOnlySecureRefreshCookie()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        var login = await RegisterAndLogin(client);

        var setCookie = RefreshSetCookie(login)!.ToLowerInvariant();
        Assert.Contains("httponly", setCookie);
        Assert.Contains("secure", setCookie);
        Assert.Contains("samesite=none", setCookie);
        Assert.Contains("path=/api/users", setCookie);
        Assert.Contains("expires=", setCookie);
    }

    [Fact]
    public async Task Refresh_RotatesTheCookieAndTheOldOneStopsWorking()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var original = CookieOf(await RegisterAndLogin(client));

        var refreshed = await Refresh(client, original);
        refreshed.EnsureSuccessStatusCode();
        var token = await refreshed.Content.ReadFromJsonAsync<AccessTokenDto>();
        var rotated = CookieOf(refreshed);

        Assert.False(string.IsNullOrEmpty(token!.AccessToken));
        Assert.NotEqual(original, rotated);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, original)).StatusCode);
    }

    [Fact]
    public async Task Refresh_WithAReusedToken_RevokesTheWholeChain()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var original = CookieOf(await RegisterAndLogin(client));
        var rotated = CookieOf(await Refresh(client, original));

        var reuse = await Refresh(client, original);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Null(RefreshSetCookie(reuse));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, rotated)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("refresh_token=unknown")]
    public async Task Refresh_WithAnAbsentOrUnknownCookie_Returns401AndSetsNoCookie(string? cookie)
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        var response = await Refresh(client, cookie);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(RefreshSetCookie(response));
    }

    [Fact]
    public async Task Refresh_WithAnExpiredCookie_Returns401()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var cookie = CookieOf(await RegisterAndLogin(client));

        await using (var context = _fixture.CreateContext())
        {
            await context.Set<RefreshTokens>().ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, cookie)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ConcurrentlyWithTheSameCookie_SucceedsExactlyOnce()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var cookie = CookieOf(await RegisterAndLogin(client));

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Refresh(client, cookie)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cookie["refresh_token=".Length..])));
        await using var context = _fixture.CreateContext();
        var userId = (await context.Set<RefreshTokens>().SingleAsync(t => t.TokenHash == hash)).AppUserId;
        Assert.False(await context.Set<RefreshTokens>().AnyAsync(t => t.AppUserId == userId && t.RevokedAtUtc == null));
    }

    [Fact]
    public async Task Refresh_AfterTheMaximumSessionLifetime_Returns401()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var cookie = CookieOf(await RegisterAndLogin(client));

        await using (var context = _fixture.CreateContext())
        {
            await context.Set<RefreshTokens>().ExecuteUpdateAsync(s => s.SetProperty(t => t.SessionStartedAtUtc, DateTime.UtcNow.AddDays(-91)));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, cookie)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheTokenAndClearsTheCookieWithoutABearer()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var cookie = CookieOf(await RegisterAndLogin(client));

        using var request = new HttpRequestMessage(HttpMethod.Post, Route(ApiEndpoints.Users.Logout));
        request.Headers.Add("Cookie", cookie);
        var logout = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", RefreshSetCookie(logout)!.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, cookie)).StatusCode);
    }

    [Fact]
    public async Task RefreshTokens_AreStoredHashed()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var raw = CookieOf(await RegisterAndLogin(client))["refresh_token=".Length..];

        await using var context = _fixture.CreateContext();
        Assert.False(await context.Set<RefreshTokens>().AnyAsync(t => t.TokenHash == raw));
    }
    [Theory]
    [InlineData("https://evil.example", HttpStatusCode.Forbidden)]
    [InlineData("http://localhost:5173", HttpStatusCode.OK)]
    public async Task Refresh_ChecksTheOriginHeader(string origin, HttpStatusCode expected)
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var cookie = CookieOf(await RegisterAndLogin(client));

        using var request = new HttpRequestMessage(HttpMethod.Post, Route(ApiEndpoints.Users.RefreshToken));
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("Origin", origin);

        Assert.Equal(expected, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Login_FromAForeignOrigin_IsRejected()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var request = new HttpRequestMessage(HttpMethod.Post, Route(ApiEndpoints.Users.LoginUser))
        {
            Content = JsonContent.Create(new LoginUserRequest("runner", Password))
        };
        request.Headers.Add("Origin", "https://evil.example");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Logout_FromAForeignOrigin_IsRejectedAndTheSessionSurvives()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var cookie = CookieOf(await RegisterAndLogin(client));

        using var request = new HttpRequestMessage(HttpMethod.Post, Route(ApiEndpoints.Users.Logout));
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("Origin", "https://evil.example");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Refresh(client, cookie)).StatusCode);
    }


    private static async Task<HttpResponseMessage> RegisterAndLogin(HttpClient client)
    {
        var username = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username, Password));
        login.EnsureSuccessStatusCode();
        return login;
    }

    private static Task<HttpResponseMessage> Refresh(HttpClient client, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Route(ApiEndpoints.Users.RefreshToken));
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        return client.SendAsync(request);
    }

    private static string? RefreshSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith("refresh_token="))
            : null;

    private static string CookieOf(HttpResponseMessage response) => RefreshSetCookie(response)!.Split(';')[0];

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));

    private static string Route(string route) => $"/{route}";
}
