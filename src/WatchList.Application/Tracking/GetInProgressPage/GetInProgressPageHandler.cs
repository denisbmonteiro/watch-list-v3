using WatchList.Application.Abstractions;
using WatchList.Application.Common;
using WatchList.Domain.Abstractions;
using WatchList.Domain.Tracking;
using WatchList.Shared.Extensions;

namespace WatchList.Application.Tracking.GetInProgressPage;

internal sealed class GetInProgressPageHandler(
    IReadRepository<InProgressEntry> entries,
    ICoverUrlResolver coverUrls) : IQueryHandler<GetInProgressPageQuery, PagedResult<InProgressItemDto>>
{
    public async Task<PagedResult<InProgressItemDto>> HandleAsync(GetInProgressPageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var all = await entries.ListAsync(cancellationToken);

        List<InProgressItemDto> filtered =
        [
            .. all
                .WhereIf(!string.IsNullOrWhiteSpace(query.Search), entry => entry.Title.Value.ContainsIgnoreCase(query.Search!))
                .OrderByIgnoreCase(entry => entry.Title.Value)
                .Select(ToDto),
        ];

        return filtered.ToPagedResult(query.Page, all.Count);
    }

    private InProgressItemDto ToDto(InProgressEntry entry) => new(
        entry.Title.Value,
        entry.MediaType,
        entry.Progress.Value,
        entry.Progress.Unit,
        entry.CoverImage is null ? null : coverUrls.Resolve(entry.MediaType, entry.CoverImage));
}
