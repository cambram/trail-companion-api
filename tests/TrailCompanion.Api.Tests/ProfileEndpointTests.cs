using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TrailCompanion.Api.Endpoints;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Infrastructure.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Api.Tests;

[Trait("Category", "Integration")]
public class ProfileEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "correct horse battery";
    private const string NewPassword = "new horse battery staple";

    private readonly PostgresDatabaseFixture _fixture;

    public ProfileEndpointTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProfileRoutes_WithoutAToken_Return401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var update = await client.PutAsJsonAsync(Route(ApiEndpoints.Users.UpdateMyUser), new UpdateProfileRequest("A", "B"));
        var password = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.ChangePassword), new ChangePasswordRequest(Password, NewPassword));
        var delete = await client.SendAsync(DeleteMe(Password));

        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, password.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task UpdateMyUser_SetsTrimsAndClearsNames()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);

        var set = await client.PutAsJsonAsync(Route(ApiEndpoints.Users.UpdateMyUser), new UpdateProfileRequest("  Kilian ", "Jornet"));
        var afterSet = await client.GetFromJsonAsync<AppUserDto>(Route(ApiEndpoints.Users.GetMyUser));
        await client.PutAsJsonAsync(Route(ApiEndpoints.Users.UpdateMyUser), new UpdateProfileRequest("   ", null));
        var afterClear = await client.GetFromJsonAsync<AppUserDto>(Route(ApiEndpoints.Users.GetMyUser));

        set.EnsureSuccessStatusCode();
        Assert.Equal(("Kilian", "Jornet"), (afterSet!.FirstName, afterSet.LastName));
        Assert.Null(afterClear!.FirstName);
        Assert.Null(afterClear.LastName);
    }

    [Fact]
    public async Task UpdateMyUser_WithAnOverlongName_Returns400()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await SignIn(client);

        var response = await client.PutAsJsonAsync(Route(ApiEndpoints.Users.UpdateMyUser), new UpdateProfileRequest(new string('a', 101), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("FirstNameTooLong", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ChangePassword_SwapsWhichPasswordLogsIn()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var username = await SignIn(client);

        var wrong = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.ChangePassword), new ChangePasswordRequest("wrong password", NewPassword));
        var weak = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.ChangePassword), new ChangePasswordRequest(Password, "short"));
        var change = await client.PostAsJsonAsync(Route(ApiEndpoints.Users.ChangePassword), new ChangePasswordRequest(Password, NewPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Contains("PasswordTooShort", await weak.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, username, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, username, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task DeleteMyUser_RemovesTheUserAndEverythingTheyOwn()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var username = await SignIn(client);
        var raceId = await CreateRaceWithSegmentAndItem(client);
        var me = await client.GetFromJsonAsync<AppUserDto>(Route(ApiEndpoints.Users.GetMyUser));

        var wrong = await client.SendAsync(DeleteMe("wrong password"));
        var delete = await client.SendAsync(DeleteMe(Password));

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, username, Password)).StatusCode);

        await using var context = _fixture.CreateContext();
        Assert.False(await context.AppUsers.AnyAsync(u => u.Id == me!.Id));
        Assert.False(await context.Set<Races>().AnyAsync(r => r.Id == raceId));
        Assert.False(await context.Set<Segments>().AnyAsync(s => s.RaceId == raceId));
        Assert.False(await context.Set<NutritionItems>().AnyAsync(n => n.AppUserId == me!.Id));
        Assert.False(await context.Set<RefreshTokens>().AnyAsync(t => t.AppUserId == me!.Id));
    }

    private static async Task<Guid> CreateRaceWithSegmentAndItem(HttpClient client)
    {
        var race = await (await client.PostAsJsonAsync(Route(ApiEndpoints.Users.CreateRace), new CreateRaceRequest("UTMB", null)))
            .Content.ReadFromJsonAsync<RaceDto>();
        var segment = await (await client.PostAsJsonAsync(
                Route(ApiEndpoints.Users.CreateSegment.Replace("{raceId:guid}", race!.Id.ToString())),
                new SegmentRequest("Seg", 30, null, null)))
            .Content.ReadFromJsonAsync<SegmentDto>();
        var me = await client.GetFromJsonAsync<AppUserDto>(Route(ApiEndpoints.Users.GetMyUser));
        var nutrition = await (await client.PostAsJsonAsync(
                Route(ApiEndpoints.Users.CreateNutritionItem),
                new NutritionItemRequest(me!.Username, NutritionType.Gel, null, 40, null, null, null, null, null, null),
                TestJson.Options))
            .Content.ReadFromJsonAsync<NutritionItemDto>(TestJson.Options);
        var item = await client.PostAsJsonAsync(
            Route(ApiEndpoints.Users.CreateItem
                .Replace("{raceId:guid}", race.Id.ToString())
                .Replace("{segmentId:guid}", segment!.Id.ToString())),
            new ItemRequest(nutrition!.Id));
        item.EnsureSuccessStatusCode();

        return race.Id;
    }

    private static HttpRequestMessage DeleteMe(string password) =>
        new(HttpMethod.Delete, Route(ApiEndpoints.Users.DeleteMyUser))
        {
            Content = JsonContent.Create(new DeleteAccountRequest(password))
        };

    private static Task<HttpResponseMessage> Login(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync(Route(ApiEndpoints.Users.LoginUser), new LoginUserRequest(username, password));

    private static async Task<string> SignIn(HttpClient client)
    {
        var username = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync(Route(ApiEndpoints.Users.RegisterUser), new RegisterUserRequest(username, Password));
        var token = await (await Login(client, username, Password)).Content.ReadFromJsonAsync<AccessTokenDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return username;
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString)
            .UseSetting("Jwt:Key", "test-signing-key-that-is-at-least-32-bytes-long"));

    private static string Route(string route) => $"/{route}";
}
