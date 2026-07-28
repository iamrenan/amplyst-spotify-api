namespace amplyst_spotify_api.Models.Spotify;

internal record Paging<T>
(
     string Href,
     int Limit,
     int Offset,
     int Total,
     string? Previous,
     string? Next,
     List<T> Items
);