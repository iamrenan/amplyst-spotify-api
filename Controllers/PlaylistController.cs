using System.Security.Claims;
using System.Text.Json;
using amplyst_spotify_api.Models.Spotify;
using amplyst_spotify_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace amplyst_spotify_api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class PlaylistController(IHttpClientFactory httpClientFactory, ITokenService tokenService, ILogger<PlaylistController> logger) : ControllerBase
{
    private static readonly string playlistsAPIUrl = "https://api.spotify.com/v1/me/playlists";

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        string? userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var tokenResponse = await tokenService.GetTokenAsync(userId);
        if (tokenResponse == null || tokenResponse.ExpiresAt <= DateTime.UtcNow)
        {
            logger.LogWarning("Access token expired or missing");
            return Unauthorized();
        }

        try
        {
            logger.LogInformation("Fetching playlists for user");
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenResponse?.AccessToken}");

            var queryParams = new Dictionary<string, string>();
            if (HttpContext.Request.Query.TryGetValue("limit", out var limitValue) && int.TryParse(limitValue, out var limit))
            {
                queryParams["limit"] = limit.ToString();
            }
            if (HttpContext.Request.Query.TryGetValue("offset", out var offsetValue) && int.TryParse(offsetValue, out var offset))
            {
                queryParams["offset"] = offset.ToString();
            }

            string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUri = queryString.Length > 0 ? $"{playlistsAPIUrl}?{queryString}" : playlistsAPIUrl;

            var response = await client.GetAsync(requestUri);
            var content = await response.Content.ReadFromJsonAsync<Paging<SimplifiedPlaylist>>(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return Ok(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching playlists: {Message}", ex.Message);
            return Problem(ex.Message);
        }
    }
}