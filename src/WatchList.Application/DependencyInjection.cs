using Microsoft.Extensions.DependencyInjection;
using WatchList.Application.Abstractions;
using WatchList.Application.Catalog.GetCatalogPage;
using WatchList.Application.Catalog.GetInProgressPage;
using WatchList.Application.Catalog.GetQueue;
using WatchList.Application.Common;
using WatchList.Application.Dashboard.GetDashboardSummary;

namespace WatchList.Application;

public static class DependencyInjection
{
    /// <summary>Registers the query handlers; the repositories and the cover resolver come from Infrastructure.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetCatalogPageQuery, PagedResult<CatalogItemDto>>, GetCatalogPageHandler>();
        services.AddScoped<IQueryHandler<GetInProgressPageQuery, PagedResult<InProgressItemDto>>, GetInProgressPageHandler>();
        services.AddScoped<IQueryHandler<GetQueueQuery, IReadOnlyList<QueueItemDto>>, GetQueueHandler>();
        services.AddScoped<IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>, GetDashboardSummaryHandler>();

        return services;
    }
}