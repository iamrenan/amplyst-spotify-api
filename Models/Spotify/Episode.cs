namespace amplyst_spotify_api.Models.Spotify;

public record Episode : TrackOrEpisode
{
    public required string? AudioPreviewUrl { get; init; }
    public required string Description { get; init; }
    public required string HtmlDescription { get; init; }
    public required int DurationMs { get; init; }
    public required bool Explicit { get; init; }
    public required Image[] Images { get; init; }
    public required bool IsExternallyHosted { get; init; }
    public required bool IsPlayable { get; init; }
    public required string[] Languages { get; init; }
    public required string Name { get; init; }
    public required string ReleaseDate { get; init; }
    public required string ReleaseDatePrecision { get; init; }
    public required Show Show { get; init; }
}

public record Show
{
    public required Copyright[] Copyrights { get; init; }
    public required string Description { get; init; }
    public required string HtmlDescription { get; init; }
    public required bool Explicit { get; init; }
    public required ExternalUrls ExternalUrls { get; init; }
    public required string Href { get; init; }
    public required string Id { get; init; }
    public required Image[] Images { get; init; }
    public required bool IsExternallyHosted { get; init; }
    public required bool IsPlayable { get; init; }
    public required string[] Languages { get; init; }
    public required string MediaType { get; init; }
    public required string Publisher { get; init; }
    public required string Name { get; init; }
    public string? Type { get; init; }
    public required string Uri { get; init; }
    public required int TotalEpisodes { get; init; }
}

public sealed record Copyright
{
    public string? Text { get; init; }
    public string? Type { get; init; }
}