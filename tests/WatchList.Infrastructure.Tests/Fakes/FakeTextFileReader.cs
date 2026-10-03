using Microsoft.Extensions.Primitives;
using WatchList.Infrastructure.Persistence.TextFiles;

namespace WatchList.Infrastructure.Tests.Fakes;

/// <summary>In-memory files; <see cref="Change"/> fires the token handed out by <see cref="Watch"/>.</summary>
internal sealed class FakeTextFileReader : ITextFileReader
{
    private readonly Dictionary<string, string[]> _files = [];
    private readonly Dictionary<string, CancellationTokenSource> _watchers = [];

    public int Reads { get; private set; }

    public FakeTextFileReader With(string fileName, params string[] lines)
    {
        _files[fileName] = lines;
        return this;
    }

    public void Change(string fileName, params string[] lines)
    {
        _files[fileName] = lines;

        if (_watchers.Remove(fileName, out var watcher))
        {
            watcher.Cancel();
            watcher.Dispose();
        }
    }

    public Task<IReadOnlyList<string>> ReadLinesAsync(string fileName, CancellationToken cancellationToken)
    {
        Reads++;
        return Task.FromResult<IReadOnlyList<string>>(_files.GetValueOrDefault(fileName) ?? []);
    }

    public IChangeToken Watch(string fileName)
    {
        if (!_watchers.TryGetValue(fileName, out var watcher))
        {
            watcher = new CancellationTokenSource();
            _watchers[fileName] = watcher;
        }

        return new CancellationChangeToken(watcher.Token);
    }
}