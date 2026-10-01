using WatchList.Shared.Results;

namespace WatchList.Domain.ValueObjects;

public enum MediaType
{
    Anime,
    Movie,
    Series,
    Book,
    Game,
    Manga,
}

public static class MediaTypeExtensions
{
    public static readonly Error Unknown = new(
        "MediaType.Unknown",
        $"Unknown media type. Expected one of: {string.Join(", ", Enum.GetNames<MediaType>())}.");

    extension(MediaType type)
    {
        /// <summary>Case-insensitive; unlike <see cref="Enum.Parse(Type, string)"/>, numeric values are rejected.</summary>
        public static Result<MediaType> Parse(string? value)
        {
            var trimmed = value?.Trim();

            foreach (var candidate in Enum.GetValues<MediaType>())
            {
                if (string.Equals(candidate.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return Unknown;
        }

        public ProgressUnit ProgressUnit => type switch
        {
            MediaType.Anime => ProgressUnit.Episode,
            MediaType.Series => ProgressUnit.SeasonEpisode,
            MediaType.Manga => ProgressUnit.Chapter,
            MediaType.Book => ProgressUnit.Page,
            MediaType.Movie or MediaType.Game => ProgressUnit.None,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}