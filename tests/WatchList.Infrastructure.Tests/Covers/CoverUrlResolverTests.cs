using WatchList.Domain.ValueObjects;
using WatchList.Infrastructure.Covers;

namespace WatchList.Infrastructure.Tests.Covers;

public sealed class CoverUrlResolverTests
{
    [Theory]
    [InlineData(MediaType.Anime, "images/anime/cover.jpg")]
    [InlineData(MediaType.Movie, "images/movie/cover.jpg")]
    [InlineData(MediaType.Series, "images/series/cover.jpg")]
    [InlineData(MediaType.Book, "images/book/cover.jpg")]
    [InlineData(MediaType.Manga, "images/manga/cover.jpg")]
    public void Maps_the_media_type_to_its_image_folder(MediaType mediaType, string expected)
    {
        var url = new CoverUrlResolver().Resolve(mediaType, CoverImage.Create("cover.jpg").Value);

        url.ShouldBe(expected);
    }
}