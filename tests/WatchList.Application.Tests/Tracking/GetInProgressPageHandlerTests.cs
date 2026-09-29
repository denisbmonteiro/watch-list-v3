using WatchList.Application.Common;
using WatchList.Application.Tests.Fakes;
using WatchList.Application.Tracking.GetInProgressPage;
using WatchList.Domain.Tracking;
using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Tests.Tracking;

public sealed class GetInProgressPageHandlerTests
{
    private static readonly GetInProgressPageHandler _handler = new(
        new InMemoryReadRepository<InProgressEntry>(
            InProgressEntry.Create("One Piece", "Manga", "531.1", null).Value,
            InProgressEntry.Create("Frieren", "Anime", "12", "frieren.jpg").Value,
            InProgressEntry.Create("Dark", "Series", "S02E03", "dark.jpg").Value),
        new FakeCoverUrlResolver());

    private static Task<PagedResult<InProgressItemDto>> HandleAsync(string? search = null, PageRequest? page = null) =>
        _handler.HandleAsync(new GetInProgressPageQuery(search, page ?? PageRequest.All), CancellationToken.None);

    [Fact]
    public async Task Maps_type_progress_and_cover_sorted_by_title()
    {
        var page = await HandleAsync();

        page.Items.ShouldBe(
        [
            new InProgressItemDto("Dark", MediaType.Series, "S02E03", ProgressUnit.SeasonEpisode, "covers/Series/dark.jpg"),
            new InProgressItemDto("Frieren", MediaType.Anime, "12", ProgressUnit.Episode, "covers/Anime/frieren.jpg"),
            new InProgressItemDto("One Piece", MediaType.Manga, "531.1", ProgressUnit.Chapter, null),
        ]);
    }

    [Fact]
    public async Task Search_and_pagination_apply()
    {
        var page = await HandleAsync("e", new PageRequest(0, 1));

        page.Items.Select(item => item.Title).ShouldBe(["Frieren"]);
        page.FilteredCount.ShouldBe(2);
        page.TotalCount.ShouldBe(3);
        page.TotalPages.ShouldBe(2);
    }
}
