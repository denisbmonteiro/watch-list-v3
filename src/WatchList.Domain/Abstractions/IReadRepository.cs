namespace WatchList.Domain.Abstractions;

public interface IReadRepository<T>
{
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken);
}