using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TrailCompanion.Infrastructure.Authentication;

internal sealed class PwnedPasswordsChecker : IBreachedPasswordChecker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PwnedPasswordsChecker> _logger;

    public PwnedPasswordsChecker(HttpClient httpClient, ILogger<PwnedPasswordsChecker> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var suffix = hash[5..];

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"range/{hash[..5]}");
            request.Headers.Add("Add-Padding", "true");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            foreach (var line in body.Split('\n'))
            {
                var parts = line.Trim().Split(':');

                if (parts.Length == 2
                    && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parts[1], out var count)
                    && count > 0)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception exception) when ((exception is HttpRequestException or TaskCanceledException)
                                          && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Breached-password check unavailable; allowing the password.");
            return false;
        }
    }
}
