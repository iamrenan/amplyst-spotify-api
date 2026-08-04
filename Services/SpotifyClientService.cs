using System.Runtime.CompilerServices;
using System.Text.Json;
using amplyst_spotify_api.Exceptions;
using amplyst_spotify_api.Models.Spotify;

namespace amplyst_spotify_api.Services;

public interface ISpotifyClientService
{
    public Task<AccessTokenResponse> RequestAccessTokenAsync(string redirectUri, string code, CancellationToken cancellationToken = default);
    public Task<AccessTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<SimplifiedPlaylist> FetchAllCurrentUserPlaylistsAsync(string accessToken, CancellationToken cancellationToken = default);
    public Task<IEnumerable<PlaylistItem>> FetchAllPlaylistItemsAsync(string accessToken, string playlistId, CancellationToken cancellationToken = default);
}

public partial class SpotifyClientService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<SpotifyClientService> logger) : ISpotifyClientService
{
    private static readonly string baseUrl = "https://api.spotify.com/v1";
    private static readonly string tokenUrl = "https://accounts.spotify.com/api/token";
    private const int MaxTokenRequestAttempts = 3;
    private readonly string clientId = configuration["Spotify:ClientId"] ?? "";
    private readonly string clientSecret = configuration["Spotify:ClientSecret"] ?? "";

    public async IAsyncEnumerable<SimplifiedPlaylist> FetchAllCurrentUserPlaylistsAsync(string accessToken, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const byte limit = 50;
        int offset = 0;
        int fetched = 0;

        // TODO: Uncomment. testing a single page of playlists for now.
        Paging<SimplifiedPlaylist>? page = null;
        while (page?.Previous == null)
        {
            string? url = $"{baseUrl}/me/playlists?limit={limit}&offset={offset}";

            page = await GetAsync<Paging<SimplifiedPlaylist>>(url, accessToken, cancellationToken);

            if (page?.Items == null || page.Items.Count == 0)
            {
                yield break;
            }

            foreach (var item in page.Items)
            {
                yield return item;
                fetched++;
            }

            if (page?.Limit < limit)
            {
                yield break;
            }

            offset += page?.Limit ?? 0;
        }
    }

    public async Task<IEnumerable<PlaylistItem>> FetchAllPlaylistItemsAsync(string accessToken, string playlistId, CancellationToken cancellationToken = default)
    {
        var all = new List<PlaylistItem>(100);
        Uri? url = new($"{baseUrl}/playlists/{playlistId}/items?limit=100");
        while (url is not null)
        {
            var page = await GetAsync<Paging<PlaylistItem>>(url.ToString(), accessToken, cancellationToken);

            if (page?.Items is not null)
            {
                all.AddRange(page.Items);
            }
            url = page?.Next is not null ? new Uri(page.Next) : null;
        }
        return all;
    }

    public Task<AccessTokenResponse> RequestAccessTokenAsync(string redirectUri, string code, CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "redirect_uri", $"{redirectUri}/api/v1/auth/callback" },
            { "code", code }
        };

        return PostTokenRequestAsync(form, cancellationToken);
    }

    public Task<AccessTokenResponse> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        return PostTokenRequestAsync(form, cancellationToken);
    }

    /// <summary>
    /// Posts a request to Spotify's token endpoint, retrying transient failures (429/5xx) up to <see cref="MaxTokenRequestAttempts"/> times.
    /// </summary>
    private async Task<AccessTokenResponse> PostTokenRequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        logger.LogInformation("POST {TokenUrl} body: {Body}", tokenUrl, DescribeTokenRequestBody(form));

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))}");

        for (var attempt = 1; attempt <= MaxTokenRequestAttempts; attempt++)
        {
            var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }, cancellationToken);

                return tokenResponse is null
                    ? throw new SpotifyAuthenticationException("Spotify token endpoint returned an empty response.")
                    : tokenResponse with { ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn) };
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var isRetryable = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;

            if (!isRetryable || attempt == MaxTokenRequestAttempts)
            {
                logger.LogError("Spotify token request failed with status {StatusCode} on attempt {Attempt}/{MaxAttempts}: {Body}", (int)response.StatusCode, attempt, MaxTokenRequestAttempts, errorBody);
                throw new SpotifyAuthenticationException($"Spotify token request failed with status {(int)response.StatusCode} after {attempt} attempt(s).");
            }

            var retryAfterSeconds = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? attempt;
            logger.LogWarning("Spotify token request failed with status {StatusCode} on attempt {Attempt}/{MaxAttempts}; retrying in {RetryAfterSeconds}s.", (int)response.StatusCode, attempt, MaxTokenRequestAttempts, retryAfterSeconds);
            await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds), cancellationToken);
        }

        throw new SpotifyAuthenticationException("Spotify token request failed after exhausting all retry attempts.");
    }

    private static string DescribeTokenRequestBody(Dictionary<string, string> form)
    {
        return string.Join("&", form.Select(kvp => $"{kvp.Key}={(kvp.Key is "code" or "refresh_token" ? "<redacted>" : kvp.Value)}"));
    }

    private async Task<T?> GetAsync<T>(string url, string accessToken, CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var response = await client.GetAsync(url, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            LogTooManyRequests(url, response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 1);
            var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 1;
            await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);
            return await GetAsync<T>(url, accessToken, cancellationToken);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new SpotifyAuthenticationException("Spotify rejected the access token (401 Unauthorized). The token has likely expired and the user must re-authenticate.");
        }

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception($"Spotify API request failed with status code {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Too many requests to {Url}; retrying in {RetryAfterSeconds}s.")]
    private partial void LogTooManyRequests(string url, double retryAfterSeconds);
}