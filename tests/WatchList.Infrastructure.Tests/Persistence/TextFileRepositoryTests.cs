using Microsoft.Extensions.Logging;
using WatchList.Domain.Catalog;
using WatchList.Infrastructure.Persistence.TextFiles;
using WatchList.Infrastructure.Persistence.TextFiles.Parsers;
using WatchList.Infrastructure.Tests.Fakes;

namespace WatchList.Infrastructure.Tests.Persistence;

public sealed class TextFileRepositoryTests
{
    private readonly ListLogger<TextFileRepository<Series>> _logger = new();

    private Task<IReadOnlyList<Series>> ListAsync(params string[] lines)
    {
        var reader = new FakeTextFileReader().With("series.txt", lines);
        var repository = new TextFileRepository<Series>(reader, new SeriesLineParser(), _logger);

        return repository.ListAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Keeps_the_file_order()
    {
        var series = await ListAsync("Dark___S03E08", "Breaking Bad___S05E16___breaking-bad.jpg");

        series.Select(show => show.Title.Value).ShouldBe(["Dark", "Breaking Bad"]);
    }

    [Fact]
    public async Task Skips_blank_lines_silently()
    {
        var series = await ListAsync("Dark___S03E08", "", "   ");

        series.ShouldHaveSingleItem();
        _logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Leaves_out_an_invalid_line_and_logs_where_it_is()
    {
        var series = await ListAsync("Dark___S03E08", "", "Broken Line___S5");

        series.ShouldHaveSingleItem().Title.Value.ShouldBe("Dark");
        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldContain("line 3 of series.txt");
        entry.Message.ShouldContain("EpisodeMarker.Invalid");
    }
}