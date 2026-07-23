namespace amplyst_spotify_api.Models;

internal record AccessTokenResponse(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken, string Scope)
{
    internal DateTime ExpiresAt => DateTime.UtcNow.AddSeconds(ExpiresIn);
}