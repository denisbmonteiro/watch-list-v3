using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Catalog;

public sealed class Book
{
    public static readonly Error EmptyAuthor = new("Book.EmptyAuthor", "Book author cannot be empty.");

    private Book(Title title, string author, CoverImage? coverImage)
    {
        Title = title;
        Author = author;
        CoverImage = coverImage;
    }

    public Title Title { get; }

    public string Author { get; }

    public CoverImage? CoverImage { get; }

    public static Result<Book> Create(string? title, string? author, string? coverImage)
    {
        var validTitle = Title.Create(title);
        if (validTitle.IsFailure)
        {
            return validTitle.Error;
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            return EmptyAuthor;
        }

        var validCover = CoverImage.CreateOptional(coverImage);
        if (validCover.IsFailure)
        {
            return validCover.Error;
        }

        return new Book(validTitle.Value, author.Trim(), validCover.Value);
    }
}
