using WatchList.Domain.Tracking;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary><c>Title___MediaType___progress___cover.jpg</c>; the cover is optional.</summary>
internal sealed class InProgressLineParser : ILineParser<InProgressEntry>
{
    public string FileName => TextFileLayout.InProgress;

    public Result<InProgressEntry> Parse(string line)
    {
        var fields = TextFileLayout.Split(line, maxFields: 4);

        return fields.IsSuccess
            ? InProgressEntry.Create(fields.Value.ElementAtOrDefault(0), fields.Value.ElementAtOrDefault(1), fields.Value.ElementAtOrDefault(2), fields.Value.ElementAtOrDefault(3))
            : fields.Error;
    }
}