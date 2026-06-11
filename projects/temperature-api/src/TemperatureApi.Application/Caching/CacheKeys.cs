namespace TemperatureApi.Application.Caching;

/// <summary>Deterministic cache key builders — keep keys here, never inline strings.</summary>
public static class CacheKeys
{
    public const string ReadingPrefix = "temperature-reading:";

    public static string Reading(int id) => $"{ReadingPrefix}{id}";
}
