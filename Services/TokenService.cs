namespace amplyst_spotify_api.Services;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using amplyst_spotify_api.Models;
using Microsoft.AspNetCore.DataProtection;

public interface ITokenService
{
    public Task StoreTokenAsync(string userId, AccessTokenResponse token, CancellationToken cancellationToken = default);
    public Task<AccessTokenResponse?> GetTokenAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// File-backed <see cref="ITokenService"/> implementation. Tokens are cached in memory for fast reads and persisted to 
/// disk (encrypted with the Data Protection API) so they survive server restarts without requiring every user to 
/// re-authenticate with Spotify.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly string _storePath;
    private readonly IDataProtector _protector;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly ILogger<TokenService> _logger;
    private readonly ConcurrentDictionary<string, AccessTokenResponse> _tokens;

    public TokenService(IDataProtectionProvider dataProtectionProvider, IWebHostEnvironment environment, ILogger<TokenService> logger)
    {
        _logger = logger;
        _protector = dataProtectionProvider.CreateProtector("amplyst-spotify-api.SpotifyTokens.v1");
        _storePath = Path.Combine(environment.ContentRootPath, "App_Data", "spotify-tokens.dat");
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
        _tokens = Load();
    }

    public async Task StoreTokenAsync(string userId, AccessTokenResponse token, CancellationToken cancellationToken = default)
    {
        _tokens[userId] = token;
        await PersistAsync(cancellationToken);
    }

    public Task<AccessTokenResponse?> GetTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        _tokens.TryGetValue(userId, out var token);
        return Task.FromResult(token);
    }

    private ConcurrentDictionary<string, AccessTokenResponse> Load()
    {
        if (!File.Exists(_storePath))
        {
            return new ConcurrentDictionary<string, AccessTokenResponse>();
        }

        try
        {
            var protectedPayload = File.ReadAllText(_storePath);
            var json = _protector.Unprotect(protectedPayload);
            var data = JsonSerializer.Deserialize<Dictionary<string, AccessTokenResponse>>(json);
            return data is null
                ? new ConcurrentDictionary<string, AccessTokenResponse>()
                : new ConcurrentDictionary<string, AccessTokenResponse>(data);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or IOException)
        {
            // Corrupted file or rotated Data Protection keys — start fresh rather than crash the app.
            _logger.LogWarning(ex, "Could not read persisted Spotify tokens; starting with an empty store.");
            return new ConcurrentDictionary<string, AccessTokenResponse>();
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(_tokens.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            var protectedPayload = _protector.Protect(json);
            await File.WriteAllTextAsync(_storePath, protectedPayload, cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
