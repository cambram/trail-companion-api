using System.Net;
using TrailCompanion.Infrastructure.Authentication;
using Microsoft.Extensions.Logging.Abstractions;

namespace TrailCompanion.Infrastructure.Tests;

public class PwnedPasswordsCheckerTests
{
    private const string Suffix = "1E4C9B93F3F0682250B6CF8331B7EE68FD8";

    [Fact]
    public async Task IsBreachedAsync_WhenTheSuffixHasACount_ReturnsTrueAndSendsOnlyThePrefix()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"0018A45C4D1DEF81644B54AB7F969B88D65:1\r\n{Suffix}:3861493\r\n")
        });

        Assert.True(await CreateChecker(handler).IsBreachedAsync("password", CancellationToken.None));
        Assert.EndsWith("/range/5BAA6", handler.LastRequest!.RequestUri!.AbsoluteUri);
        Assert.Equal("true", handler.LastRequest.Headers.GetValues("Add-Padding").Single());
    }

    [Theory]
    [InlineData("0018A45C4D1DEF81644B54AB7F969B88D65:1")]
    [InlineData(Suffix + ":0")]
    public async Task IsBreachedAsync_WhenTheSuffixIsAbsentOrPadding_ReturnsFalse(string body)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });

        Assert.False(await CreateChecker(handler).IsBreachedAsync("password", CancellationToken.None));
    }

    [Fact]
    public async Task IsBreachedAsync_WhenTheServiceIsDown_FailsOpen()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("down"));

        Assert.False(await CreateChecker(handler).IsBreachedAsync("password", CancellationToken.None));
    }

    [Fact]
    public async Task IsBreachedAsync_WhenTheServiceTimesOut_FailsOpen()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("timeout"));

        Assert.False(await CreateChecker(handler).IsBreachedAsync("password", CancellationToken.None));
    }

    private static PwnedPasswordsChecker CreateChecker(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.pwnedpasswords.com/") },
            NullLogger<PwnedPasswordsChecker>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_respond(request));
        }
    }
}
