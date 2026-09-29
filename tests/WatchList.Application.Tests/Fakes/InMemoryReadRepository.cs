using WatchList.Domain.Abstractions;

namespace WatchList.Application.Tests.Fakes;

internal sealed class InMemoryReadRepository<T>(params IEnumerable<T> items) : IReadRepository<T>
{
    private readonly List<T> _items = [.. items];

    public Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<T>>(_items);
}
