using System.Collections.Concurrent;

namespace WinCCDashboard.Application.Reports;

/// <summary>
/// Small in-process TTL cache for data that rarely changes: SSRS parameter
/// metadata and filter lookup lists. Registered as a singleton.
/// Failed lookups are not cached, so a temporary outage recovers on its own.
/// </summary>
public sealed class ReportMetadataCache
{
    private readonly ConcurrentDictionary<string, (DateTime ExpiresUtc, object Value)> _items =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<T> GetOrAddAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
        where T : class
    {
        if (_items.TryGetValue(key, out var entry)
            && entry.ExpiresUtc > DateTime.UtcNow
            && entry.Value is T cached)
        {
            return cached;
        }

        var value = await factory(cancellationToken);

        if (ttl > TimeSpan.Zero)
            _items[key] = (DateTime.UtcNow.Add(ttl), value);

        return value;
    }
}
