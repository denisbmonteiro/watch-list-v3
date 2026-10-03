using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WatchList.Application;
using WatchList.Application.Abstractions;
using WatchList.Application.Catalog.GetCatalogPage;
using WatchList.Application.Common;
using WatchList.Application.Dashboard.GetDashboardSummary;
using WatchList.Application.Tracking.GetQueue;
using WatchList.Domain.ValueObjects;
using WatchList.Infrastructure.Tests.Fakes;

namespace WatchList.Infrastructure.Tests;

/// <summary>Application handlers on top of the real Infrastructure, reading the files in <c>Fixtures/</c>.</summary>
public sealed class DependencyInjectionTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public DependencyInjectionTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:DataPath"] = "Fixtures" })
            .Build();

        _provider = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(new FakeHostEnvironment(AppContext.BaseDirectory))
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public void Dispose() => _provider.Dispose();

    private Task<TResult> HandleAsync<TQuery, TResult>(TQuery query)
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();

        return handler.HandleAsync(query, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Dashboard_counts_every_valid_line_of_every_file()
    {
        var summary = await HandleAsync<GetDashboardSummaryQuery, DashboardSummaryDto>(new GetDashboardSummaryQuery());

        // series.txt has an invalid line and games.txt a blank one; neither is counted.
        summary.ShouldBe(new DashboardSummaryDto(InProgress: 3, Anime: 3, Book: 2, Game: 3, Manga: 2, Movie: 2, Queue: 2, Series: 2));
    }

    [Fact]
    public async Task Catalog_resolves_cover_urls_into_the_images_folder()
    {
        var page = await HandleAsync<GetCatalogPageQuery, PagedResult<CatalogItemDto>>(
            new GetCatalogPageQuery(MediaType.Series, Search: null, PageRequest.All));

        page.Items.ShouldBe(
        [
            new CatalogItemDto("Breaking Bad", "images/series/breaking-bad.jpg", EpisodeMarker: "S05E16"),
            new CatalogItemDto("Dark", CoverUrl: null, EpisodeMarker: "S03E08"),
        ]);
    }

    [Fact]
    public async Task Queue_file_with_a_byte_order_mark_keeps_its_first_title_clean()
    {
        var queue = await HandleAsync<GetQueueQuery, IReadOnlyList<QueueItemDto>>(new GetQueueQuery());

        queue.ShouldBe([new QueueItemDto("Kotoha no Niwa", MediaType.Movie), new QueueItemDto("Shirobako", MediaType.Anime)]);
    }
}