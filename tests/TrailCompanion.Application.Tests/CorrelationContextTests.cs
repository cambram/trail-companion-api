using TrailCompanion.Application.Common.Services;

namespace TrailCompanion.Application.Tests;

public class CorrelationContextTests
{
    private readonly CorrelationContext _context = new();

    [Fact]
    public void CorrelationId_OutsideAnyScope_IsUnassigned()
    {
        Assert.Equal(CorrelationContext.Unassigned, _context.CorrelationId);
    }

    [Fact]
    public void Begin_SetsTheAmbientIdAndRestoresItOnDispose()
    {
        using (_context.Begin("request-1"))
        {
            Assert.Equal("request-1", _context.CorrelationId);
        }

        Assert.Equal(CorrelationContext.Unassigned, _context.CorrelationId);
    }

    [Fact]
    public void Begin_Nested_RestoresTheOuterIdOnInnerDispose()
    {
        using (_context.Begin("outer"))
        {
            using (_context.Begin("inner"))
            {
                Assert.Equal("inner", _context.CorrelationId);
            }

            Assert.Equal("outer", _context.CorrelationId);
        }
    }

    [Fact]
    public async Task Begin_FlowsIntoAwaitedWork()
    {
        using (_context.Begin("across-await"))
        {
            await Task.Yield();
            await Task.Delay(1);

            Assert.Equal("across-await", _context.CorrelationId);
        }
    }

    [Fact]
    public void Begin_WithAnUnusableId_Throws()
    {
        Assert.Throws<ArgumentException>(() => _context.Begin("has spaces"));
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("a-b_c.d")]
    [InlineData("A")]
    public void IsAcceptable_AcceptsSafeIdentifiers(string candidate)
    {
        Assert.True(CorrelationContext.IsAcceptable(candidate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has spaces")]
    [InlineData("new\nline")]
    [InlineData("semi;colon")]
    [InlineData("unassigned")]
    [InlineData("UNASSIGNED")]
    public void IsAcceptable_RejectsUnsafeOrReservedInput(string? candidate)
    {
        Assert.False(CorrelationContext.IsAcceptable(candidate));
    }

    [Fact]
    public void IsAcceptable_RejectsAnythingLongerThanTheMaximum()
    {
        var tooLong = new string('a', CorrelationContext.MaxLength + 1);

        Assert.False(CorrelationContext.IsAcceptable(tooLong));
        Assert.True(CorrelationContext.IsAcceptable(tooLong[..CorrelationContext.MaxLength]));
    }

    [Fact]
    public void Sanitize_PassesAcceptableInputThroughUnchanged()
    {
        Assert.Equal("caller-supplied-1", CorrelationContext.Sanitize("caller-supplied-1"));
    }

    [Fact]
    public void Sanitize_ReplacesUnacceptableInputWithAFreshId()
    {
        var sanitized = CorrelationContext.Sanitize("log\ninjection attempt");

        Assert.True(CorrelationContext.IsAcceptable(sanitized));
        Assert.DoesNotContain('\n', sanitized);
    }

    [Fact]
    public void NewId_ProducesDistinctAcceptableIds()
    {
        var first = CorrelationContext.NewId();
        var second = CorrelationContext.NewId();

        Assert.NotEqual(first, second);
        Assert.True(CorrelationContext.IsAcceptable(first));
    }
}
