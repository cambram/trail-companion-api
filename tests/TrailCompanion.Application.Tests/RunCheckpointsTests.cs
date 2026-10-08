using TrailCompanion.Application.Modules.Users;
using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Tests;

public class RunCheckpointsTests
{
    private readonly RunScenario _s = new();

    [Fact]
    public void Build_OrdersStationsBySegmentPositionAndEndsWithTheFinish()
    {
        var shuffled = new List<AidStations> { _s.As3, _s.As1, _s.As2 };

        var checkpoints = RunCheckpoints.Build(_s.SegmentRows, shuffled);

        Assert.Equal(["AS1", "AS2", "AS3", "Finish"], checkpoints.Select(c => c.Name));
        Assert.Equal([0, 1, 2, 3], checkpoints.Select(c => c.Index));
        Assert.Equal(RunCheckpoints.FinishKey, checkpoints[^1].Key);
        Assert.Null(checkpoints[^1].AidStationId);
        Assert.Equal(_s.As1.Id.ToString(), checkpoints[0].Key);
    }

    [Fact]
    public void Build_WithNoStations_IsJustTheFinish()
    {
        var checkpoints = RunCheckpoints.Build(_s.SegmentRows, []);

        Assert.Equal(RunCheckpoints.FinishKey, Assert.Single(checkpoints).Key);
    }

    [Fact]
    public void Build_IgnoresAStationWhoseSegmentIsGone()
    {
        var orphan = new AidStations { Id = Guid.NewGuid(), SegmentId = Guid.NewGuid(), Name = "Gone" };

        var checkpoints = RunCheckpoints.Build(_s.SegmentRows, [orphan, _s.As1]);

        Assert.Equal(["AS1", "Finish"], checkpoints.Select(c => c.Name));
    }

    [Fact]
    public void Find_ResolvesAStationIdAndTheFinishCaseInsensitively()
    {
        var checkpoints = RunCheckpoints.Build(_s.SegmentRows, _s.StationRows);

        Assert.Equal("AS2", RunCheckpoints.Find(checkpoints, _s.As2.Id.ToString())!.Name);
        Assert.Equal("Finish", RunCheckpoints.Find(checkpoints, " FINISH ")!.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public void Find_UnknownKey_ReturnsNull(string? key)
    {
        var checkpoints = RunCheckpoints.Build(_s.SegmentRows, _s.StationRows);

        Assert.Null(RunCheckpoints.Find(checkpoints, key));
    }

    [Fact]
    public void TryParseKey_AcceptsGuidAndFinishOnly()
    {
        Assert.True(RunCheckpoints.TryParseKey("finish", out var finish));
        Assert.Null(finish);
        Assert.True(RunCheckpoints.TryParseKey(_s.As1.Id.ToString(), out var station));
        Assert.Equal(_s.As1.Id, station);
        Assert.False(RunCheckpoints.TryParseKey("AS1", out _));
    }

    [Fact]
    public void KeyOf_UsesTheStationIdOrFinish()
    {
        Assert.Equal(_s.As1.Id.ToString(), RunCheckpoints.KeyOf(new Arrivals { AidStationId = _s.As1.Id }));
        Assert.Equal("finish", RunCheckpoints.KeyOf(new Arrivals { AidStationId = null }));
    }
}
