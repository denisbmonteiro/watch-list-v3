using Microsoft.AspNetCore.Mvc;
using WatchList.Api.Contracts;
using WatchList.Application.Abstractions;
using WatchList.Application.Catalog.GetCatalogPage;
using WatchList.Application.Common;
using WatchList.Domain.ValueObjects;

namespace WatchList.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(
    IQueryHandler<GetCatalogPageQuery, PagedResult<CatalogItemDto>> handler) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CatalogItemDto>> ListAsync([FromQuery] ListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return handler.HandleAsync(new(MediaType.Game, query.Search, query.ToPageRequest()), cancellationToken);
    }
}