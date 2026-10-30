using Microsoft.AspNetCore.Mvc;
using WatchList.Api.Contracts;
using WatchList.Application.Abstractions;
using WatchList.Application.Catalog.GetCatalogPage;
using WatchList.Application.Common;
using WatchList.Domain.ValueObjects;

namespace WatchList.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController(
    IQueryHandler<GetCatalogPageQuery, PagedResult<CatalogItemDto>> handler) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CatalogItemDto>> ListAsync([FromQuery] ListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return handler.HandleAsync(new(MediaType.Movie, query.Search, query.ToPageRequest()), cancellationToken);
    }
}