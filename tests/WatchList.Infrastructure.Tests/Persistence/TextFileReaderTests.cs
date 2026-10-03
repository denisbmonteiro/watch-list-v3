using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using WatchList.Infrastructure.Persistence.TextFiles;
using WatchList.Infrastructure.Tests.Fakes;

namespace WatchList.Infrastructure.Tests.Persistence;

public sealed class TextFileReaderTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("watchlist-");
    private readonly PhysicalFileProvider _files;
    private readonly ListLogger<TextFileReader> _logger = new();
    private readonly TextFileReader _reader;

    public TextFileReaderTests()
    {
        _files = new PhysicalFileProvider(_directory.FullName);
        _reader = new TextFileReader(_files, _logger);
    }

    public void Dispose()
    {
        _files.Dispose();
        _directory.Delete(recursive: true);
    }

    private Task<IReadOnlyList<string>> ReadAsync(byte[] content)
    {
        File.WriteAllBytes(Path.Combine(_directory.FullName, "list.txt"), content);
        return _reader.ReadLinesAsync("list.txt", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Drops_the_byte_order_mark()
    {
        var lines = await ReadAsync([0xEF, 0xBB, 0xBF, .. "Shirobako___Anime"u8]);

        lines.ShouldBe(["Shirobako___Anime"]);
    }

    [Fact]
    public async Task Splits_on_crlf_without_leaving_carriage_returns()
    {
        var lines = await ReadAsync([.. "Hades\r\nCeleste\r\n"u8]);

        lines.ShouldBe(["Hades", "Celeste"]);
    }

    [Fact]
    public async Task Keeps_blank_lines_so_line_numbers_match_the_file()
    {
        var lines = await ReadAsync([.. "Hades\n\nCeleste"u8]);

        lines.ShouldBe(["Hades", "", "Celeste"]);
    }

    [Fact]
    public async Task Reads_utf8_titles()
    {
        var lines = await ReadAsync([.. "Pokémon: Mewtwo Strikes Back—Evolution"u8]);

        lines.ShouldBe(["Pokémon: Mewtwo Strikes Back—Evolution"]);
    }

    [Fact]
    public async Task Missing_file_is_an_empty_list_with_a_warning()
    {
        var lines = await _reader.ReadLinesAsync("missing.txt", TestContext.Current.CancellationToken);

        lines.ShouldBeEmpty();
        _logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }
}