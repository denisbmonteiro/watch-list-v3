using WatchList.Domain.Catalog;
using WatchList.Domain.ValueObjects;

namespace WatchList.Domain.Tests.Catalog;

public sealed class CatalogEntityTests
{
    [Fact]
    public void Anime_with_cover()
    {
        var anime = Anime.Create("86 (2021)", "86-2021.jpg").Value;

        anime.Title.Value.ShouldBe("86 (2021)");
        anime.CoverImage!.FileName.ShouldBe("86-2021.jpg");
    }

    [Fact]
    public void Movie_still_without_cover() =>
        Movie.Create("Suzume no Tojimari", null).Value.CoverImage.ShouldBeNull();

    [Fact]
    public void Movie_with_invalid_cover_fails() =>
        Movie.Create("2012", "2012.png").Error.ShouldBe(CoverImage.Invalid);

    [Fact]
    public void Series_reads_the_episode_marker()
    {
        var series = Series.Create("Breaking Bad", "S05E16", "breaking-bad.jpg").Value;

        series.EpisodeMarker.ShouldBe(new EpisodeMarker(5, 16));
        series.CoverImage!.FileName.ShouldBe("breaking-bad.jpg");
    }

    [Fact]
    public void Series_without_marker_fails() =>
        Series.Create("Breaking Bad", null, null).Error.ShouldBe(EpisodeMarker.Invalid);

    [Fact]
    public void Book_keeps_the_author()
    {
        var book = Book.Create("Overlord 1: The Undead King", " Kugane Maruyama ", null).Value;

        book.Author.ShouldBe("Kugane Maruyama");
        book.CoverImage.ShouldBeNull();
    }

    [Fact]
    public void Book_without_author_fails() =>
        Book.Create("Overlord 1: The Undead King", "", null).Error.ShouldBe(Book.EmptyAuthor);

    [Fact]
    public void Game_and_manga_have_only_a_title()
    {
        Game.Create("Borderlands: The Pre-Sequel").Value.Title.Value.ShouldBe("Borderlands: The Pre-Sequel");
        Manga.Create("Koe no Katachi").Value.Title.Value.ShouldBe("Koe no Katachi");
    }

    [Fact]
    public void Empty_title_fails_for_every_entity()
    {
        Anime.Create(" ", null).Error.ShouldBe(Title.Empty);
        Movie.Create(" ", null).Error.ShouldBe(Title.Empty);
        Series.Create(" ", "S01E01", null).Error.ShouldBe(Title.Empty);
        Book.Create(" ", "Author", null).Error.ShouldBe(Title.Empty);
        Game.Create(" ").Error.ShouldBe(Title.Empty);
        Manga.Create(" ").Error.ShouldBe(Title.Empty);
    }
}
