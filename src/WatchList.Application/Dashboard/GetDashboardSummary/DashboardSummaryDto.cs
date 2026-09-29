namespace WatchList.Application.Dashboard.GetDashboardSummary;

/// <summary>How many titles each list holds.</summary>
public sealed record DashboardSummaryDto(
    int InProgress,
    int Anime,
    int Book,
    int Game,
    int Manga,
    int Movie,
    int Queue,
    int Series);
