using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Catalog;

public sealed class Anime
{
    private Anime(Title title, CoverImage? coverImage)
    {
        Title = title;
        CoverImage = coverImage;
    }

    public Title Title { get; }

    public CoverImage? CoverImage { get; }

    public static Result<Anime> Create(string? title, string? coverImage)
    {
        var validTitle = Title.Create(title);
        if (validTitle.IsFailure)
        {
            return validTitle.Error;
        }

        var validCover = CoverImage.CreateOptional(coverImage);
        if (validCover.IsFailure)
        {
            return validCover.Error;
        }

        return new Anime(validTitle.Value, validCover.Value);
    }
}