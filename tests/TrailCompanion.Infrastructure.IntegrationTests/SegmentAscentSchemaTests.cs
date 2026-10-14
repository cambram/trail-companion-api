using TrailCompanion.Domain.Modules.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TrailCompanion.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public class SegmentAscentSchemaTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;

    public SegmentAscentSchemaTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task ValidAscent_IsStored(int? ascent)
    {
        var seed = await RunSeed.Race(_fixture);

        await using var context = _fixture.CreateContext();
        var segment = new Segments { RaceId = seed.Race.Id, Position = 2, Name = "S2", AscentM = ascent };
        context.Add(segment);
        await context.SaveChangesAsync();

        Assert.Equal(ascent, await context.Set<Segments>().Where(s => s.Id == segment.Id).Select(s => s.AscentM).SingleAsync());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10001)]
    public async Task OutOfRangeAscent_ViolatesTheCheckConstraint(int ascent)
    {
        var seed = await RunSeed.Race(_fixture);

        await using var context = _fixture.CreateContext();
        context.Add(new Segments { RaceId = seed.Race.Id, Position = 2, Name = "S2", AscentM = ascent });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Equal("CK_Segments_AscentM", ((PostgresException)exception.InnerException!).ConstraintName);
    }
}
