using amplyst_spotify_api.Data;
using amplyst_spotify_api.Logging;
using amplyst_spotify_api.Repositories;
using amplyst_spotify_api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "logs", "log.txt")));

builder.Services
    .AddOpenApi()
    .AddHttpClient()
    .AddHttpLogging(static options =>
    {
        options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.ResponseTrailers;
        options.RequestHeaders.Add("X-Correlation-Id");
        options.MediaTypeOptions.AddText("application/json");
        options.RequestBodyLogLimit = 4096;
        options.ResponseBodyLogLimit = 4096;
        options.CombineLogs = true;
    })
    .AddMemoryCache()
    .AddDbContext<AmplystDbContext>(static options => options.UseInMemoryDatabase("AmplystDatabase"))
    .AddSingleton<ITokenService, TokenService>()
    .AddScoped<IImportService, ImportService>()
    .AddScoped<IImportRepository, ImportRepository>()
    .AddScoped<ILibraryService, LibraryService>()
    .AddScoped<ILibraryRepository, LibraryRepository>()
    .AddScoped<ISpotifyClientService, SpotifyClientService>();

builder.Services.AddControllers()
    .AddJsonOptions(static options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

string dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");

builder.Services.AddDataProtection()
    .SetApplicationName("amplyst-spotify-api")
    .PersistKeysToFileSystem(new(dataProtectionKeysPath));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(static options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "SpotifyAuthCookie";
        options.LoginPath = "/api/v1/auth";
    });

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.MapOpenApi();
}

app.UseHttpsRedirection()
    .UseAuthentication()
    .UseAuthorization();

app.MapControllers();

app.Run();
