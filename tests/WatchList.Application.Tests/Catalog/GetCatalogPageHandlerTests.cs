using WatchList.Application.Catalog.GetCatalogPage;
using WatchList.Application.Common;
using WatchList.Application.Tests.Fakes;
using WatchList.Domain.Catalog;
using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Tests.Catalog;

public sealed class GetCatalogPageHandlerTests
{
    private static readonly GetCatalogPageHandler _handler = new(
        new InMemoryReadRepository<Anime>(
            Anime.Create("Steins;Gate", "steins-gate.jpg").Value,
            Anime.Create("86 (2021)", "86-2021.jpg").Value,
            Anime.Create("naruto", null).Value),
        new InMemoryReadRepository<Movie>(
            Movie.Create("Interstellar", "interstellar.jpg").Value,
            Movie.Create("Suzume no Tojimari", null).Value),
        new InMemoryReadRepository<Series>(Series.Create("Breaking Bad", "S05E16", "breaking-bad.jpg").Value),
        new InMemoryReadRepository<Book>(Book.Create("Overlord 1: The Undead King", "Kugane Maruyama", null).Value),
        new InMemoryReadRepository<Game>(Game.Create("Hades").Value),
        new InMemoryReadRepository<Manga>(Manga.Create("Koe no Katachi").Value),
        new FakeCoverUrlResolver());

    private static Task<PagedResult<CatalogItemDto>> HandleAsync(MediaType mediaType, string? search = null, PageRequest? page = null) =>
        _handler.HandleAsync(new GetCatalogPageQuery(mediaType, search, page ?? PageRequest.All), CancellationToken.None);

    [Fact]
    public async Task Sorts_by_title_ignoring_case()
    {
        var page = await HandleAsync(MediaType.Anime);

        page.Items.Select(item => item.Title).ShouldBe(["86 (2021)", "naruto", "Steins;Gate"]);
    }

    [Fact]
    public async Task Resolves_the_cover_url_and_leaves_it_null_without_a_cover()
    {
        var page = await HandleAsync(MediaType.Movie);

        page.Items.ShouldBe(
        [
            new CatalogItemDto("Interstellar", "covers/Movie/interstellar.jpg"),
            new CatalogItemDto("Suzume no Tojimari", null),
        ]);
    }

    [Fact]
    public async Task Search_matches_part_of_the_title_ignoring_case()
    {
        var page = await HandleAsync(MediaType.Anime, "GATE");

        page.Items.Select(item => item.Title).ShouldBe(["Steins;Gate"]);
        page.FilteredCount.ShouldBe(1);
        page.TotalCount.ShouldBe(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_search_returns_everything(string? search) =>
        (await HandleAsync(MediaType.Anime, search)).FilteredCount.ShouldBe(3);

    [Fact]
    public async Task Search_without_matches_returns_an_empty_page()
    {
        var page = await HandleAsync(MediaType.Anime, "zzz");

        page.Items.ShouldBeEmpty();
        page.TotalPages.ShouldBe(1);
        page.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Pages_after_searching_and_sorting()
    {
        var page = await HandleAsync(MediaType.Anime, page: new PageRequest(1, 2));

        page.Items.Select(item => item.Title).ShouldBe(["Steins;Gate"]);
        page.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task Series_carry_the_episode_marker() =>
        (await HandleAsync(MediaType.Series)).Items.ShouldBe(
            [new CatalogItemDto("Breaking Bad", "covers/Series/breaking-bad.jpg", EpisodeMarker: "S05E16")]);

    [Fact]
    public async Task Books_carry_the_author() =>
        (await HandleAsync(MediaType.Book)).Items.ShouldBe(
            [new CatalogItemDto("Overlord 1: The Undead King", null, Author: "Kugane Maruyama")]);

    [Fact]
    public async Task Games_and_mangas_have_no_cover()
    {
        (await HandleAsync(MediaType.Game)).Items.ShouldBe([new CatalogItemDto("Hades", null)]);
        (await HandleAsync(MediaType.Manga)).Items.ShouldBe([new CatalogItemDto("Koe no Katachi", null)]);
    }
}
