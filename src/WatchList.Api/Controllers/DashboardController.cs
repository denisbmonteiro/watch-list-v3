using Microsoft.AspNetCore.Mvc;
using WatchList.Application.Abstractions;
using WatchList.Application.Dashboard.GetDashboardSummary;

namespace WatchList.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(
    IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto> handler) : ControllerBase
{
    [HttpGet]
    public Task<DashboardSummaryDto> GetAsync(CancellationToken cancellationToken)
    {
        return handler.HandleAsync(new GetDashboardSummaryQuery(), cancellationToken);
    }
}