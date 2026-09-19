using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Template.Api.Common.Configuration.DependencyInjection;
using Template.Api.Endpoints;
using Template.Application.Modules.Users.Dtos;
using Template.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Template.Api.Tests;

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
    public async Task RegisterThenLogin_IssuesATokenThatReachesTheProtectedEndpoint()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(email, Password));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(email.ToUpperInvariant(), Password));
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<AccessTokenDto>();

        using var request = new HttpRequestMessage(HttpMethod.Get, Route(ApiEndpoints.Users.GetMyUser));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var me = await client.SendAsync(request);

        me.EnsureSuccessStatusCode();
        Assert.Equal(email, (await me.Content.ReadFromJsonAsync<AppUserDto>())!.Email);
    }

    [Fact]
    public async Task Register_WithAnExistingEmail_Returns409()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var body = new RegisterUserRequest(NewEmail(), Password);

        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), body);
        var second = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_Returns401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var email = NewEmail();

        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(email, Password));

        var wrongPassword = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(email, "wrong password!"));
        var unknownEmail = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(NewEmail(), Password));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
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
    public async Task Login_BeyondThePermitLimit_Returns429()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var body = new LoginUserRequest(NewEmail(), Password);

        for (var attempt = 0; attempt < RateLimitingConfiguration.LoginPermitLimit; attempt++)
        {
            await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), body);
        }

        var limited = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), body);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));

    private static string Route(string route) => $"/{route}";

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";
}
