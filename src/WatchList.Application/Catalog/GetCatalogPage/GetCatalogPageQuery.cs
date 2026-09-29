using WatchList.Application.Common;
using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Catalog.GetCatalogPage;

/// <param name="Search">Matched against the title, ignoring case; blank returns everything.</param>
public sealed record GetCatalogPageQuery(MediaType MediaType, string? Search, PageRequest Page);
