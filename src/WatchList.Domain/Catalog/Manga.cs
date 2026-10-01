using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Catalog;

public sealed class Manga
{
    private Manga(Title title) => Title = title;

    public Title Title { get; }

    public static Result<Manga> Create(string? title)
    {
        var validTitle = Title.Create(title);

        return validTitle.IsSuccess ? new Manga(validTitle.Value) : validTitle.Error;
    }
}