using MudBlazor;
using WatchList.Domain.ValueObjects;

namespace WatchList.Presentation.Formatting;

public static class MediaTypeColors
{
    public static Color For(MediaType mediaType) => mediaType switch
    {
        MediaType.Anime => Color.Primary,
        MediaType.Book => Color.Success,
        MediaType.Manga => Color.Secondary,
        MediaType.Movie => Color.Warning,
        MediaType.Series => Color.Info,
        MediaType.Game => Color.Tertiary,
        _ => Color.Default,
    };
}
