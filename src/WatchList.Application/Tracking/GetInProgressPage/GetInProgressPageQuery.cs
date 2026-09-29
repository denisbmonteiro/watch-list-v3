using WatchList.Application.Common;

namespace WatchList.Application.Tracking.GetInProgressPage;

/// <param name="Search">Matched against the title, ignoring case; blank returns everything.</param>
public sealed record GetInProgressPageQuery(string? Search, PageRequest Page);
