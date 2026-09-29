using WatchList.Application.Abstractions;
using WatchList.Domain.Abstractions;
using WatchList.Domain.Catalog;
using WatchList.Domain.Tracking;

namespace WatchList.Application.Dashboard.GetDashboardSummary;

internal sealed class GetDashboardSummaryHandler(
    IReadRepository<InProgressEntry> inProgress,
    IReadRepository<Anime> animes,
    IReadRepository<Book> books,
    IReadRepository<Game> games,
    IReadRepository<Manga> mangas,
    IReadRepository<Movie> movies,
    IReadRepository<QueueEntry> queue,
    IReadRepository<Series> series) : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> HandleAsync(GetDashboardSummaryQuery query, CancellationToken cancellationToken) =>
        new(
            InProgress: await CountAsync(inProgress, cancellationToken),
            Anime: await CountAsync(animes, cancellationToken),
            Book: await CountAsync(books, cancellationToken),
            Game: await CountAsync(games, cancellationToken),
            Manga: await CountAsync(mangas, cancellationToken),
            Movie: await CountAsync(movies, cancellationToken),
            Queue: await CountAsync(queue, cancellationToken),
            Series: await CountAsync(series, cancellationToken));

    private static async Task<int> CountAsync<T>(IReadRepository<T> repository, CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Count;
}
