using WatchList.Domain.Tracking;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary><c>Title___MediaType</c>.</summary>
internal sealed class QueueLineParser : ILineParser<QueueEntry>
{
    public string FileName => TextFileLayout.Queue;

    public Result<QueueEntry> Parse(string line)
    {
        var fields = TextFileLayout.Split(line, maxFields: 2);

        return fields.IsSuccess
            ? QueueEntry.Create(fields.Value.ElementAtOrDefault(0), fields.Value.ElementAtOrDefault(1))
            : fields.Error;
    }
}