using WatchList.Shared.Results;

namespace WatchList.Infrastructure.Persistence.TextFiles;

/// <summary>One title per line, fields separated by <see cref="Separator"/>.</summary>
internal static class TextFileLayout
{
    public const string Separator = "___";

    public const string Anime = "anime.txt";
    public const string Books = "books.txt";
    public const string Games = "games.txt";
    public const string InProgress = "index.txt";
    public const string Manga = "manga.txt";
    public const string Movies = "movies.txt";
    public const string Queue = "queue.txt";
    public const string Series = "series.txt";

    /// <summary>Missing trailing fields are fine (the entity decides); extra ones are an error.</summary>
    public static Result<string[]> Split(string line, int maxFields)
    {
        ArgumentNullException.ThrowIfNull(line);

        var fields = line.Split(Separator);

        return fields.Length <= maxFields
            ? fields
            : new Error("TextFile.TooManyFields", $"Expected at most {maxFields} fields separated by '{Separator}', found {fields.Length}.");
    }
}