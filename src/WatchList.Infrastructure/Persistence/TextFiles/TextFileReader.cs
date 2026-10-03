using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using WatchList.Infrastructure.Options;

namespace WatchList.Infrastructure.Persistence.TextFiles;

internal sealed partial class TextFileReader(
    [FromKeyedServices(StorageOptions.SectionName)] IFileProvider files,
    ILogger<TextFileReader> logger) : ITextFileReader
{
    public async Task<IReadOnlyList<string>> ReadLinesAsync(string fileName, CancellationToken cancellationToken)
    {
        var file = files.GetFileInfo(fileName);
        if (!file.Exists)
        {
            LogMissingFile(fileName);
            return [];
        }

        // UTF-8 with or without BOM (queue.txt has one); ReadLineAsync splits on \n, \r\n and \r.
        await using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        List<string> lines = [];
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }

    public IChangeToken Watch(string fileName) => files.Watch(fileName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Data file {FileName} not found; the list is empty")]
    private partial void LogMissingFile(string fileName);
}