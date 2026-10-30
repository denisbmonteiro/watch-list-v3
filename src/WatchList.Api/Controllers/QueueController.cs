using Microsoft.AspNetCore.Mvc;
using WatchList.Application.Abstractions;
using WatchList.Application.Catalog.GetQueue;

namespace WatchList.Api.Controllers;

/// <summary>The whole queue, sorted by title and not paged, like the <c>/queue</c> page.</summary>
[ApiController]
[Route("api/queue")]
public sealed class QueueController(IQueryHandler<GetQueueQuery, IReadOnlyList<QueueItemDto>> handler) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<QueueItemDto>> ListAsync(CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetQueueQuery(), cancellationToken);
}