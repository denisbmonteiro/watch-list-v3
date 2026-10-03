using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

internal interface ILineParser<T>
{
    string FileName { get; }

    Result<T> Parse(string line);
}