using Microsoft.AspNetCore.Mvc;
using WatchList.Api.Contracts;
using WatchList.Application.Abstractions;
using WatchList.Application.Catalog.GetInProgressPage;
using WatchList.Application.Common;

namespace WatchList.Api.Controllers;

/// <summary>What the home page shows: the titles in progress and how far along each one is.</summary>
[ApiController]
[Route("api/in-progress")]
public sealed class InProgressController(
    IQueryHandler<GetInProgressPageQuery, PagedResult<InProgressItemDto>> handler) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<InProgressItemDto>> ListAsync([FromQuery] ListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return handler.HandleAsync(new(query.Search, query.ToPageRequest()), cancellationToken);
    }
}