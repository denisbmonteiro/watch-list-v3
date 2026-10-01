using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Catalog;

public sealed class Series
{
    private Series(Title title, EpisodeMarker episodeMarker, CoverImage? coverImage)
    {
        Title = title;
        EpisodeMarker = episodeMarker;
        CoverImage = coverImage;
    }

    public Title Title { get; }

    public EpisodeMarker EpisodeMarker { get; }

    public CoverImage? CoverImage { get; }

    public static Result<Series> Create(string? title, string? episodeMarker, string? coverImage)
    {
        var validTitle = Title.Create(title);
        if (validTitle.IsFailure)
        {
            return validTitle.Error;
        }

        var validMarker = EpisodeMarker.Parse(episodeMarker);
        if (validMarker.IsFailure)
        {
            return validMarker.Error;
        }

        var validCover = CoverImage.CreateOptional(coverImage);
        if (validCover.IsFailure)
        {
            return validCover.Error;
        }

        return new Series(validTitle.Value, validMarker.Value, validCover.Value);
    }
}