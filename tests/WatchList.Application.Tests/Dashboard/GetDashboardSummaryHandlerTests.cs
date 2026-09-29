using WatchList.Application.Dashboard.GetDashboardSummary;
using WatchList.Application.Tests.Fakes;
using WatchList.Domain.Catalog;
using WatchList.Domain.Tracking;

namespace WatchList.Application.Tests.Dashboard;

public sealed class GetDashboardSummaryHandlerTests
{
    [Fact]
    public async Task Counts_every_list_by_name()
    {
        var handler = new GetDashboardSummaryHandler(
            new InMemoryReadRepository<InProgressEntry>(InProgressEntry.Create("Frieren", "Anime", "12", null).Value),
            new InMemoryReadRepository<Anime>(Anime.Create("A", null).Value, Anime.Create("B", null).Value),
            new InMemoryReadRepository<Book>(Book.Create("A", "Author", null).Value),
            new InMemoryReadRepository<Game>(),
            new InMemoryReadRepository<Manga>(Manga.Create("A").Value, Manga.Create("B").Value, Manga.Create("C").Value),
            new InMemoryReadRepository<Movie>(Movie.Create("A", null).Value),
            new InMemoryReadRepository<QueueEntry>(QueueEntry.Create("A", "Movie").Value, QueueEntry.Create("B", "Anime").Value),
            new InMemoryReadRepository<Series>(Series.Create("A", "S01E01", null).Value));

        var summary = await handler.HandleAsync(new GetDashboardSummaryQuery(), CancellationToken.None);

        summary.ShouldBe(new DashboardSummaryDto(InProgress: 1, Anime: 2, Book: 1, Game: 0, Manga: 3, Movie: 1, Queue: 2, Series: 1));
    }
}
