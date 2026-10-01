using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Catalog;

public sealed class Movie
{
    private Movie(Title title, CoverImage? coverImage)
    {
        Title = title;
        CoverImage = coverImage;
    }

    public Title Title { get; }

    public CoverImage? CoverImage { get; }

    public static Result<Movie> Create(string? title, string? coverImage)
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

        return new Movie(validTitle.Value, validCover.Value);
    }
}