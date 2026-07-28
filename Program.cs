using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using amplyst_spotify_api.Services;
using amplyst_spotify_api.Data;
using Microsoft.EntityFrameworkCore;
using amplyst_spotify_api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenService, TokenService>();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
builder.Services.AddDataProtection()
    .SetApplicationName("amplyst-spotify-api")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddDbContext<AmplystDbContext>(options =>
    options.UseInMemoryDatabase("AmplystDatabase"));
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "SpotifyAuthCookie";
        options.LoginPath = "/api/v1/auth";
    });

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LoggerMiddleware>();
app.MapControllers();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

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