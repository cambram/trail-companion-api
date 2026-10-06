using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TrailCompanion.Api.Common.Configuration.DependencyInjection;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class AuthFlowTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";

    private readonly PostgresDatabaseFixture _fixture;

    public AuthFlowTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_BeyondThePermitLimit_Returns429()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < RateLimitingConfiguration.RegisterPermitLimit; attempt++)
        {
            await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(NewUsername(), Password));
        }

        var limited = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(NewUsername(), Password));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task RegisterThenLogin_IssuesATokenThatReachesTheProtectedEndpoint()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var username = NewUsername();

        var register = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username.ToUpperInvariant(), Password));
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route(ApiEndpoints.Users.GetMyUser));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var me = await client.SendAsync(request);

        me.EnsureSuccessStatusCode();
        Assert.Equal(username, (await me.Content.ReadFromJsonAsync<AppUserDto>())!.Username);
    }

    [Fact]
    public async Task Register_WithAnExistingUsername_Returns409()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var body = new RegisterUserRequest(NewUsername(), Password);

        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), body);
        var second = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WhenRegistrationDisabled_Returns403ProblemAndCreatesNoUser()
    {
        using var factory = CreateFactory(registrationEnabled: false);
        using var client = factory.CreateClient();
        var body = new RegisterUserRequest(NewUsername(), Password);

        var register = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), body);

        Assert.Equal(HttpStatusCode.Forbidden, register.StatusCode);
        Assert.Equal("application/problem+json", register.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        Assert.Equal("Account creation is currently closed.", problem.RootElement.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(problem.RootElement.GetProperty("correlationId").GetString()));

        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(body.Username, Password));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_WhenRegistrationDisabled_StillWorksForExistingUsers()
    {
        var username = NewUsername();
        using (var openFactory = CreateFactory())
        using (var openClient = openFactory.CreateClient())
        {
            var register = await openClient.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        }

        using var closedFactory = CreateFactory(registrationEnabled: false);
        using var client = closedFactory.CreateClient();

        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username, Password));
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route(ApiEndpoints.Users.GetMyUser));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var me = await client.SendAsync(request);

        me.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownUsername_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var username = NewUsername();

        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));

        var wrongPassword = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username, "wrong password!"));
        var unknownUsername = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(NewUsername(), Password));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUsername.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-jwt")]
    public async Task GetMyUser_WithoutAValidToken_Returns401(string? token)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route(ApiEndpoints.Users.GetMyUser));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithACaseOnlyDifferentUsername_Returns409()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var username = NewUsername();

        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
        var second = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username.ToUpperInvariant(), Password));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        using var problem = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal("That username is taken.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Register_WithAnInvalidUsername_Returns400()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest("bad name", Password));

        Assert.Equal(HttpStatusCode.BadRequest, register.StatusCode);
        Assert.Contains("InvalidUsername", await register.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithAnEmailShapedUsername_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest("me@example.com", Password));

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_BeyondThePermitLimit_Returns429()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var body = new LoginUserRequest(NewUsername(), Password);

        for (var attempt = 0; attempt < RateLimitingConfiguration.LoginPermitLimit; attempt++)
        {
            await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), body);
        }

        var limited = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), body);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task Login_RateLimit_IsCountedPerForwardedClientAddress()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var body = new LoginUserRequest(NewUsername(), Password);

        for (var attempt = 0; attempt < RateLimitingConfiguration.LoginPermitLimit; attempt++)
        {
            await client.SendAsync(LoginFrom("203.0.113.1", body));
        }

        var limited = await client.SendAsync(LoginFrom("203.0.113.1", body));
        var otherClient = await client.SendAsync(LoginFrom("203.0.113.2", body));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherClient.StatusCode);
    }

    private static HttpRequestMessage LoginFrom(string address, LoginUserRequest body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Route(ApiEndpoints.Users.LoginUser))
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-Forwarded-For", address);
        return request;
    }

    private WebApplicationFactory<Program> CreateFactory(bool registrationEnabled = true) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long")
            .UseSetting("FeatureManagement:RegistrationEnabled", registrationEnabled ? "true" : "false"));

    private static string Route(string route) => $"/{route}";

    private static string NewUsername() => Guid.NewGuid().ToString("N");
}
