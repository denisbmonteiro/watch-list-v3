using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WatchList.Api.Contracts;
using WatchList.Application.Catalog.GetCatalogPage;
using WatchList.Application.Catalog.GetInProgressPage;
using WatchList.Application.Catalog.GetQueue;
using WatchList.Application.Common;
using WatchList.Domain.ValueObjects;

namespace WatchList.Api.Tests;

public sealed class ListEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<T> GetAsync<T>(string url)
        where T : class
    {
        var result = await _client.GetFromJsonAsync<T>(url, _json, TestContext.Current.CancellationToken);

        return result.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("/api/anime", 3)]
    [InlineData("/api/movies", 2)]
    [InlineData("/api/series", 2)]
    [InlineData("/api/books", 2)]
    [InlineData("/api/games", 3)]
    [InlineData("/api/manga", 2)]
    public async Task Catalog_routes_list_every_valid_line_of_their_file(string url, int count)
    {
        var page = await GetAsync<PagedResult<CatalogItemDto>>(url);

        page.TotalCount.ShouldBe(count);
        page.Items.Count.ShouldBe(count);
        page.PageSize.ShouldBe(ListQuery.DefaultPageSize);
    }

    [Fact]
    public async Task Catalog_searches_and_pages_like_the_presentation()
    {
        var page = await GetAsync<PagedResult<CatalogItemDto>>("/api/anime?search=GATE&pageIndex=0&pageSize=1");

        page.Items.ShouldBe([new CatalogItemDto("Steins;Gate", "images/anime/steins-gate.jpg")]);
        (page.PageIndex, page.PageSize, page.FilteredCount, page.TotalCount).ShouldBe((0, 1, 1, 3));
    }

    [Fact]
    public async Task In_progress_returns_media_type_and_progress_unit_as_text()
    {
        using var response = await _client.GetAsync(new Uri("/api/in-progress", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.ShouldContain("\"mediaType\":\"Anime\"");
        body.ShouldContain("\"progressUnit\":\"SeasonEpisode\"");

        var page = JsonSerializer.Deserialize<PagedResult<InProgressItemDto>>(body, _json).ShouldNotBeNull();
        page.Items.Select(item => item.Title).ShouldBe(["Breaking Bad", "One Piece", "Tales of Demons and Gods"]);
    }

    [Fact]
    public async Task Queue_returns_the_whole_list_sorted_by_title()
    {
        var queue = await GetAsync<List<QueueItemDto>>("/api/queue");

        queue.ShouldBe([new QueueItemDto("Kotoha no Niwa", MediaType.Movie), new QueueItemDto("Shirobako", MediaType.Anime)]);
    }

    [Theory]
    [InlineData("/api/anime?pageIndex=-1")]
    [InlineData("/api/movies?pageSize=0")]
    [InlineData("/api/series?pageSize=101")]
    [InlineData("/api/in-progress?pageSize=abc")]
    public async Task Out_of_range_paging_returns_a_validation_problem(string url)
    {
        using var response = await _client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    public async Task Api_reference_is_served_in_development(string url)
    {
        using var response = await _client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unknown_route_returns_not_found()
    {
        using var response = await _client.GetAsync(new Uri("/api/dashboard", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}