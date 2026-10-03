using WatchList.Domain.Catalog;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary><c>Title___S05E16___cover.jpg</c>; the cover is optional.</summary>
internal sealed class SeriesLineParser : ILineParser<Series>
{
    public string FileName => TextFileLayout.Series;

    public Result<Series> Parse(string line)
    {
        var fields = TextFileLayout.Split(line, maxFields: 3);

        return fields.IsSuccess
            ? Series.Create(fields.Value.ElementAtOrDefault(0), fields.Value.ElementAtOrDefault(1), fields.Value.ElementAtOrDefault(2))
            : fields.Error;
    }
}