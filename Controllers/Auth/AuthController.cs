using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using amplyst_spotify_api.Models;
using amplyst_spotify_api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace amplyst_spotify_api.Controllers.Auth;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ITokenService tokenService, IMemoryCache stateCache) : ControllerBase
{
    private readonly string redirectUri = configuration["RedirectUri"] ?? "https://127.0.0.1:7138";
    private readonly string clientId = configuration["Spotify:ClientId"] ?? "";
    private readonly string clientSecret = configuration["Spotify:ClientSecret"] ?? "";
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    [HttpGet]
    public async Task<IActionResult> GetAuth()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            userId = Guid.NewGuid().ToString();
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        string state = GenerateRandomString(16);
        stateCache.Set(StateCacheKey(userId), state, StateLifetime);

        const string scope = "user-read-private user-read-email playlist-read-private";

        var queryParams = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "client_id", clientId },
            { "scope", scope },
            { "redirect_uri", $"{redirectUri}/api/v1/auth/callback" },
            { "state", state },
            { "show_dialog", "true" }
        };

        string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        string authorizationUrl = $"https://accounts.spotify.com/authorize?{queryString}";

        return Redirect(authorizationUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string? code, string? state, string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest(error);
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            // _logger.LogWarning("User ID not found in callback; redirecting to auth.");
            return Redirect("/api/v1/auth");
        }

        string stateKey = StateCacheKey(userId);
        if (!stateCache.TryGetValue(stateKey, out string? expectedState) || string.IsNullOrEmpty(state) || state != expectedState)
        {
            return BadRequest("Unexpected state");
        }
        stateCache.Remove(stateKey);

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("Missing authorization code");
        }

        var token = await RequestAccessToken(code);
        if (token is null)
        {
            return Problem("Failed to obtain access token from Spotify.");
        }

        await tokenService.StoreTokenAsync(userId, token);

        return Ok(new { token.ExpiresAt, Playlists = $"{redirectUri}/api/v1/playlist" });
    }

    private async Task<AccessTokenResponse?> RequestAccessToken(string code)
    {
        const string tokenUrl = "https://accounts.spotify.com/api/token";
        var form = new Dictionary<string, string>   {
            { "grant_type", "authorization_code" },
            { "redirect_uri", $"{redirectUri}/api/v1/auth/callback" },
            { "code", code }
        };

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))}");

        var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form));

        var tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return tokenResponse is null
            ? null
            : tokenResponse with { ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn) };
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        return new string([.. Enumerable.Range(0, length).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])]);
    }

    private static string StateCacheKey(string userId) => $"spotify-oauth-state:{userId}";
}
