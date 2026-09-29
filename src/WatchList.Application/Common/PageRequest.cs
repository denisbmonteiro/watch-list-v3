namespace WatchList.Application.Common;

/// <param name="PageIndex">Zero-based; past the last page it is clamped to the last one.</param>
/// <param name="PageSize"><see cref="int.MaxValue"/> means every item on a single page.</param>
public sealed record PageRequest(int PageIndex, int PageSize)
{
    public static PageRequest All { get; } = new(0, int.MaxValue);

    public int PageIndex { get; } = PageIndex >= 0
        ? PageIndex
        : throw new ArgumentOutOfRangeException(nameof(PageIndex), PageIndex, "Page index cannot be negative.");

    public int PageSize { get; } = PageSize > 0
        ? PageSize
        : throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, "Page size must be positive.");
}
