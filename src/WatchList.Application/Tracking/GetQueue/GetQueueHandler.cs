using WatchList.Application.Abstractions;
using WatchList.Domain.Abstractions;
using WatchList.Domain.Tracking;
using WatchList.Shared.Extensions;

namespace WatchList.Application.Tracking.GetQueue;

internal sealed class GetQueueHandler(IReadRepository<QueueEntry> entries)
    : IQueryHandler<GetQueueQuery, IReadOnlyList<QueueItemDto>>
{
    public async Task<IReadOnlyList<QueueItemDto>> HandleAsync(GetQueueQuery query, CancellationToken cancellationToken)
    {
        var all = await entries.ListAsync(cancellationToken);

        return
        [
            .. all
                .OrderByIgnoreCase(entry => entry.Title.Value)
                .Select(entry => new QueueItemDto(entry.Title.Value, entry.MediaType)),
        ];
    }
}
