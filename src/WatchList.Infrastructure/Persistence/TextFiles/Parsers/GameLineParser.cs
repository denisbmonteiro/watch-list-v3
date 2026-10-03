using WatchList.Domain.Catalog;
using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles.Parsers;

/// <summary>The whole line is the title.</summary>
internal sealed class GameLineParser : ILineParser<Game>
{
    public string FileName => TextFileLayout.Games;

    public Result<Game> Parse(string line) => Game.Create(line);
}