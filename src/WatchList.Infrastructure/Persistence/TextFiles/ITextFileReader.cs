using Microsoft.Extensions.Primitives;

namespace WatchList.Infrastructure.Persistence.TextFiles;

internal interface ITextFileReader
{
    /// <summary>Every line as stored, blank ones included, so line numbers stay meaningful; a missing file has none.</summary>
    Task<IReadOnlyList<string>> ReadLinesAsync(string fileName, CancellationToken cancellationToken);

    /// <summary>Fires when the file is created, changed or deleted.</summary>
    IChangeToken Watch(string fileName);
}