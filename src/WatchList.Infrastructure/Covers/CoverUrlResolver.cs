using WatchList.Application.Abstractions;
using WatchList.Domain.ValueObjects;

namespace WatchList.Infrastructure.Covers;

/// <summary>Covers live in <c>wwwroot/images/{folder}/</c>; the URL is relative to the app base.</summary>
internal sealed class CoverUrlResolver : ICoverUrlResolver
{
    public string Resolve(MediaType mediaType, CoverImage coverImage)
    {
        ArgumentNullException.ThrowIfNull(coverImage);

        return $"images/{Folder(mediaType)}/{coverImage.FileName}";
    }

    private static string Folder(MediaType mediaType) => mediaType switch
    {
        MediaType.Anime => "anime",
        MediaType.Movie => "movie",
        MediaType.Series => "series",
        MediaType.Book => "book",
        MediaType.Game => "game",
        MediaType.Manga => "manga",
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, null),
    };
}