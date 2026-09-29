using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Catalog;

public sealed class Game
{
    private Game(Title title) => Title = title;

    public Title Title { get; }

    public static Result<Game> Create(string? title)
    {
        var validTitle = Title.Create(title);

        return validTitle.IsSuccess ? new Game(validTitle.Value) : validTitle.Error;
    }
}
