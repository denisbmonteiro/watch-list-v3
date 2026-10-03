using WatchList.Domain.Catalog;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary>The whole line is the title.</summary>
internal sealed class MangaLineParser : ILineParser<Manga>
{
    public string FileName => TextFileLayout.Manga;

    public Result<Manga> Parse(string line) => Manga.Create(line);
}