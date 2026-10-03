using Microsoft.Extensions.Logging;
using WatchList.Domain.Abstractions;
using WatchList.Infrastructure.Persistence.TextFiles.Parsers;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles;

internal sealed class TextFileRepository<T>(
    ITextFileReader reader,
    ILineParser<T> parser,
    ILogger<TextFileRepository<T>> logger) : IReadRepository<T>
{
    /// <summary>Blank lines are skipped; an invalid line is logged and left out instead of failing the whole list.</summary>
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken)
    {
        var lines = await reader.ReadLinesAsync(parser.FileName, cancellationToken);

        List<T> items = [];
        for (var index = 0; index < lines.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                continue;
            }

            var result = parser.Parse(lines[index]);
            if (result.IsSuccess)
            {
                items.Add(result.Value);
            }
            else
            {
                TextFileLog.InvalidLine(logger, parser.FileName, index + 1, result.Error);
            }
        }

        return items;
    }
}

internal static partial class TextFileLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping line {LineNumber} of {FileName}: {Error}")]
    public static partial void InvalidLine(ILogger logger, string fileName, int lineNumber, Error error);
}