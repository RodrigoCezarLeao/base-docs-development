namespace DocMap.Application.Caching;

/// <summary>Deterministic cache key builders — keep keys here, never inline strings.</summary>
public static class CacheKeys
{
    public const string ProjectPrefix = "project:";

    public static string Project(int id) => $"{ProjectPrefix}{id}";
}
