using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Tracking.GetInProgressPage;

/// <param name="Progress">The raw value (<c>12</c>, <c>531.1</c>, <c>S05E16</c>); the label is up to the UI.</param>
public sealed record InProgressItemDto(
    string Title,
    MediaType MediaType,
    string Progress,
    ProgressUnit ProgressUnit,
    string? CoverUrl);
