using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DevBeast.Mcp.Server.Services;

public interface ICacheService
{
    Task<IReadOnlyList<CacheEntry>> InspectAsync(string? keyPattern = "*", CancellationToken cancellationToken = default);
    Task<bool> FlushKeyAsync(string key, CancellationToken cancellationToken = default);
    bool IsMockMode { get; }
}

public sealed class RedisCacheService(IOptions<DevBeastOptions> options) : ICacheService, IDisposable
{
    private readonly Dictionary<string, (string Value, TimeSpan? Ttl)> _mockStore = new()
    {
        ["cache:products:all"] = ("""{"count":3,"items":["SKU-001","SKU-002","SKU-003"]}""", TimeSpan.FromMinutes(15)),
        ["cache:user:jan.kowalski@example.com"] = ("""{"tier":"Gold","lastLogin":"2025-07-01T10:00:00Z"}""", TimeSpan.FromHours(1)),
        ["session:abc123"] = ("""{"userId":"user-42","roles":["Customer"]}""", TimeSpan.FromMinutes(30))
    };

    private IConnectionMultiplexer? _redis;
    private bool _useMock;

    public bool IsMockMode => _useMock;

    public async Task<IReadOnlyList<CacheEntry>> InspectAsync(string? keyPattern = "*", CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync();

        if (_useMock)
        {
            return _mockStore
                .Where(kvp => MatchPattern(kvp.Key, keyPattern ?? "*"))
                .Select(kvp => new CacheEntry(kvp.Key, kvp.Value.Value, DetectType(kvp.Value.Value), kvp.Value.Ttl))
                .ToList();
        }

        var db = _redis!.GetDatabase();
        var server = _redis.GetServers().First();
        var keys = server.Keys(pattern: keyPattern ?? "*").Take(100).ToArray();
        var entries = new List<CacheEntry>();

        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = await db.StringGetAsync(key);
            var ttl = await db.KeyTimeToLiveAsync(key);
            entries.Add(new CacheEntry(key!, value.HasValue ? value.ToString() : null, DetectType(value.ToString()), ttl));
        }

        return entries;
    }

    public async Task<bool> FlushKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync();

        if (_useMock)
        {
            return _mockStore.Remove(key);
        }

        var db = _redis!.GetDatabase();
        return await db.KeyDeleteAsync(key);
    }

    private async Task EnsureConnectionAsync()
    {
        if (_redis is not null || _useMock) return;

        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(options.Value.Redis.ConnectionString);
            _useMock = false;
        }
        catch
        {
            if (options.Value.Redis.UseMockWhenUnavailable)
            {
                _useMock = true;
            }
            else
            {
                throw;
            }
        }
    }

    private static bool MatchPattern(string key, string pattern)
    {
        if (pattern == "*") return true;
        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(key, regex);
    }

    private static string? DetectType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            using var doc = JsonDocument.Parse(value);
            return doc.RootElement.ValueKind switch
            {
                JsonValueKind.Object => "json:object",
                JsonValueKind.Array => "json:array",
                _ => "json:scalar"
            };
        }
        catch
        {
            return "string";
        }
    }

    public void Dispose() => _redis?.Dispose();
}
