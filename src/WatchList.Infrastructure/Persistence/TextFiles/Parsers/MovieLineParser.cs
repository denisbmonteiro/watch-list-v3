using WatchList.Domain.Catalog;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary><c>Title___cover.jpg</c>; the cover is optional.</summary>
internal sealed class MovieLineParser : ILineParser<Movie>
{
    public string FileName => TextFileLayout.Movies;

    public Result<Movie> Parse(string line)
    {
        var fields = TextFileLayout.Split(line, maxFields: 2);

        return fields.IsSuccess
            ? Movie.Create(fields.Value.ElementAtOrDefault(0), fields.Value.ElementAtOrDefault(1))
            : fields.Error;
    }
}