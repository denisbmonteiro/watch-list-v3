using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using WatchList.Domain.Catalog;
using WatchList.Infrastructure.Caching;
using WatchList.Infrastructure.Persistence.TextFiles;
using WatchList.Infrastructure.Persistence.TextFiles.Parsers;
using WatchList.Infrastructure.Tests.Fakes;

namespace WatchList.Infrastructure.Tests.Caching;

public sealed class CachedReadRepositoryTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly FakeTextFileReader _reader = new FakeTextFileReader().With("games.txt", "Hades");
    private readonly CachedReadRepository<Game> _repository;

    public CachedReadRepositoryTests()
    {
        var parser = new GameLineParser();
        var inner = new TextFileRepository<Game>(_reader, parser, NullLogger<TextFileRepository<Game>>.Instance);
        _repository = new CachedReadRepository<Game>(inner, parser, _reader, _cache);
    }

    public void Dispose() => _cache.Dispose();

    private Task<IReadOnlyList<Game>> ListAsync() => _repository.ListAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task Reads_the_file_once_while_it_does_not_change()
    {
        await ListAsync();
        var games = await ListAsync();

        games.ShouldHaveSingleItem().Title.Value.ShouldBe("Hades");
        _reader.Reads.ShouldBe(1);
    }

    [Fact]
    public async Task Reads_again_after_the_file_changes()
    {
        await ListAsync();
        _reader.Change("games.txt", "Hades", "Celeste");

        var games = await ListAsync();

        games.Count.ShouldBe(2);
        _reader.Reads.ShouldBe(2);
    }
}