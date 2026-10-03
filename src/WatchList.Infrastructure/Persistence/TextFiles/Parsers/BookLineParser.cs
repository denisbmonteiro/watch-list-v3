using WatchList.Domain.Catalog;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary><c>Title___Author___cover.jpg</c>; the cover is optional.</summary>
internal sealed class BookLineParser : ILineParser<Book>
{
    public string FileName => TextFileLayout.Books;

    public Result<Book> Parse(string line)
    {
        var fields = TextFileLayout.Split(line, maxFields: 3);

        return fields.IsSuccess
            ? Book.Create(fields.Value.ElementAtOrDefault(0), fields.Value.ElementAtOrDefault(1), fields.Value.ElementAtOrDefault(2))
            : fields.Error;
    }
}