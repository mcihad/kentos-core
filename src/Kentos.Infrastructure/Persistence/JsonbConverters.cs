using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kentos.Infrastructure.Persistence;

/// <summary>Value converters/comparers for storing CLR types as jsonb.</summary>
public static class JsonbConverters
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<Dictionary<string, object?>, string> MetadataConverter =
        new(
            v => JsonSerializer.Serialize(v, Options),
            v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, Options) ?? new Dictionary<string, object?>());

    public static readonly ValueComparer<Dictionary<string, object?>> MetadataComparer =
        new(
            (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(StringComparison.Ordinal),
            v => JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(v, Options), Options)!);

    /// <summary>Converter storing any type as jsonb (string).</summary>
    public static ValueConverter<T, string> Json<T>() =>
        new(
            v => JsonSerializer.Serialize(v, Options),
            v => JsonSerializer.Deserialize<T>(v, Options)!);

    /// <summary>Value comparer for types stored as jsonb.</summary>
    public static ValueComparer<T> JsonComparer<T>() =>
        new(
            (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(StringComparison.Ordinal),
            v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options)!);
}
