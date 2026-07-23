using amplyst_spotify_api.Models;
using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var app = builder.Build();
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<LoggerMiddleware>();

string redirectUri = builder.Configuration["RedirectUri"] ?? "https://127.0.0.1:7138";
string clientId = builder.Configuration["Spotify:ClientId"] ?? "";
string clientSecret = builder.Configuration["Spotify:ClientSecret"] ?? "";
string? currentState = null;
AccessTokenResponse? tokenResponse = null;

app.MapGet("/get/playlists", async (HttpContext context, ILogger<Program> logger) =>
{
    if (tokenResponse == null || tokenResponse.ExpiresAt <= DateTime.UtcNow)
    {
        logger.LogWarning("Access token expired or missing");
        return Results.Unauthorized();
    }

    try
    {
        logger.LogInformation("Fetching playlists for user");
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");

        var queryParams = new Dictionary<string, string>();
        if (context.Request.Query.TryGetValue("limit", out var limitValue) && int.TryParse(limitValue, out var limit))
        {
            queryParams["limit"] = limit.ToString();
        }
        if (context.Request.Query.TryGetValue("offset", out var offsetValue) && int.TryParse(offsetValue, out var offset))
        {
            queryParams["offset"] = offset.ToString();
        }

        string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var requestUri = queryString.Length > 0
            ? $"https://api.spotify.com/v1/me/playlists?{queryString}"
            : "https://api.spotify.com/v1/me/playlists";

        var response = await client.GetAsync(requestUri);
        var content = await response.Content.ReadFromJsonAsync<Paging<SimplifiedPlaylist>>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        return Results.Json(content);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching playlists: {Message}", ex.Message);
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/auth", () =>
{
    currentState = GenerateRandomString(16);
    const string scope = "user-read-private user-read-email playlist-read-private";

    var queryParams = new Dictionary<string, string>
    {
        { "response_type", "code" },
        { "client_id", clientId },
        { "scope", scope },
        { "redirect_uri", $"{redirectUri}/auth/callback" },
        { "state", currentState },
        { "show_dialog", "true" }
    };

    string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

    string authorizationUrl = $"https://accounts.spotify.com/authorize?{queryString}";

    return Results.Redirect(authorizationUrl);
})
.WithName("GetAuthCallback");


app.MapGet("/auth/callback", async (string? code, string? state, string? error) =>
{
    if (!string.IsNullOrEmpty(error))
    {
        return Results.BadRequest(error);
    }

    if (state != currentState)
    {
        return Results.BadRequest("Unexpected state");
    }

    if (string.IsNullOrEmpty(code))
    {
        return Results.BadRequest("Missing authorization code");
    }

    await RequestAccessToken(code);

    return Results.Ok(new { ExpiresAt = tokenResponse?.ExpiresAt, Playlists = $"{redirectUri}/get/playlists" });
});

app.Run();

async Task RequestAccessToken(string code)
{
    const string tokenUrl = "https://accounts.spotify.com/api/token";
    var form = new Dictionary<string, string>   {
        { "grant_type", "authorization_code" },
        { "redirect_uri", $"{redirectUri}/auth/callback" },
        { "code", code }
    };

    using var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))}");

    var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form));

    tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    });


}

string GenerateRandomString(int length)
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    return new string([.. Enumerable.Range(0, length).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])]);
}

/// Middleware that logs the request and response 
internal class LoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggerMiddleware> _logger;

    public LoggerMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        // Log the request
        _logger.LogInformation("Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);
        // Call the next middleware in the pipeline
        await _next(context);
        // Log the response
        _logger.LogInformation("Finished handling request {Method} {Path}. Response status code: {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
}