using WatchList.Application.Tests.Fakes;
using WatchList.Application.Tracking.GetQueue;
using WatchList.Domain.Tracking;
using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Tests.Tracking;

public sealed class GetQueueHandlerTests
{
    [Fact]
    public async Task Returns_the_whole_queue_sorted_by_title()
    {
        var handler = new GetQueueHandler(new InMemoryReadRepository<QueueEntry>(
            QueueEntry.Create("Perfect Blue", "Movie").Value,
            QueueEntry.Create("mushishi", "Anime").Value));

        var queue = await handler.HandleAsync(new GetQueueQuery(), CancellationToken.None);

        queue.ShouldBe(
        [
            new QueueItemDto("mushishi", MediaType.Anime),
            new QueueItemDto("Perfect Blue", MediaType.Movie),
        ]);
    }
}
