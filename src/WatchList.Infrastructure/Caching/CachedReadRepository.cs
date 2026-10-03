using Microsoft.Extensions.Caching.Memory;
using WatchList.Domain.Abstractions;
using WatchList.Infrastructure.Persistence.TextFiles;
using WatchList.Infrastructure.Persistence.TextFiles.Parsers;

namespace WatchList.Infrastructure.Caching;

/// <summary>Keeps the parsed list in memory until its <c>.txt</c> changes on disk.</summary>
internal sealed class CachedReadRepository<T>(
    IReadRepository<T> inner,
    ILineParser<T> parser,
    ITextFileReader reader,
    IMemoryCache cache) : IReadRepository<T>
{
    // Static per closed generic type, so each list gets its own entry.
    private static readonly object _cacheKey = new();

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(_cacheKey, async entry =>
        {
            // Watch before reading, so a change made during the read still evicts the entry.
            entry.AddExpirationToken(reader.Watch(parser.FileName));
            return await inner.ListAsync(cancellationToken);
        }) ?? [];
}