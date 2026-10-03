using WatchList.Domain.Catalog;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary><c>Title___cover.jpg</c>; the cover is optional.</summary>
internal sealed class AnimeLineParser : ILineParser<Anime>
{
    public string FileName => TextFileLayout.Anime;

    public Result<Anime> Parse(string line)
    {
        var fields = TextFileLayout.Split(line, maxFields: 2);

        return fields.IsSuccess
            ? Anime.Create(fields.Value.ElementAtOrDefault(0), fields.Value.ElementAtOrDefault(1))
            : fields.Error;
    }
}