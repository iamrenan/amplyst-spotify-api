using System.Text.Json;
using System.Text.Json.Serialization;

namespace amplyst_spotify_api.Models.Spotify;

[JsonConverter(typeof(TrackOrEpisodeConverter))]
public abstract record TrackOrEpisode
{
    public ExternalUrls? ExternalUrls { get; init; }
    public required string Href { get; init; }
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Uri { get; init; }
}

public class TrackOrEpisodeConverter : JsonConverter<TrackOrEpisode>
{
    public override TrackOrEpisode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var typeDiscriminator = root.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString()
            : null;

        // Avoids recursion when deserializing
        var innerFields = new JsonSerializerOptions(options);
        foreach (var option in innerFields.Converters)
        {
            if (option is TrackOrEpisodeConverter)
            {
                innerFields.Converters.Remove(option);
                continue;
            }
        }

        return typeDiscriminator switch
        {
            "track" => root.Deserialize<Track>(innerFields),
            "episode" => root.Deserialize<Episode>(innerFields),
            _ => throw new JsonException($"Unknown or missing type discriminator '{typeDiscriminator}' for TrackOrEpisode.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TrackOrEpisode value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}