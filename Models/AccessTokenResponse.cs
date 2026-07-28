namespace amplyst_spotify_api.Models;

public record AccessTokenResponse(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken, string Scope)
{
    public DateTime ExpiresAt { get; init; }
}