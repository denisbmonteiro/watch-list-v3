using WatchList.Domain.Catalog;
using WatchList.Domain.ValueObjects;
using WatchList.Infrastructure.Persistence.TextFiles.Parsers;

namespace WatchList.Infrastructure.Tests.Parsers;

public sealed class LineParserTests
{
    [Fact]
    public void Anime_reads_title_and_cover()
    {
        var anime = new AnimeLineParser().Parse("Steins;Gate___steins-gate.jpg").Value;

        anime.Title.Value.ShouldBe("Steins;Gate");
        anime.CoverImage!.FileName.ShouldBe("steins-gate.jpg");
    }

    [Fact]
    public void Movie_without_cover_has_no_cover()
    {
        var movie = new MovieLineParser().Parse("Suzume no Tojimari").Value;

        movie.Title.Value.ShouldBe("Suzume no Tojimari");
        movie.CoverImage.ShouldBeNull();
    }

    [Fact]
    public void Series_reads_the_episode_marker()
    {
        var series = new SeriesLineParser().Parse("Breaking Bad___S05E16___breaking-bad.jpg").Value;

        series.EpisodeMarker.ShouldBe(new EpisodeMarker(5, 16));
        series.CoverImage!.FileName.ShouldBe("breaking-bad.jpg");
    }

    [Fact]
    public void Series_missing_the_episode_marker_fails()
    {
        var result = new SeriesLineParser().Parse("Dark");

        result.Error.ShouldBe(EpisodeMarker.Invalid);
    }

    [Fact]
    public void Book_reads_the_author()
    {
        var book = new BookLineParser().Parse("Overlord 1: The Undead King___Kugane Maruyama___overlord-1.jpg").Value;

        book.Author.ShouldBe("Kugane Maruyama");
    }

    [Fact]
    public void Book_missing_the_author_fails()
    {
        var result = new BookLineParser().Parse("Overlord 1: The Undead King");

        result.Error.ShouldBe(Book.EmptyAuthor);
    }

    [Theory]
    [InlineData("One Piece___Anime___1179___one-piece.jpg", MediaType.Anime, "1179", ProgressUnit.Episode)]
    [InlineData("Tales of Demons and Gods___Manga___531.1___tales-of-demons-and-gods.jpg", MediaType.Manga, "531.1", ProgressUnit.Chapter)]
    [InlineData("Breaking Bad___Series___S05E16", MediaType.Series, "S05E16", ProgressUnit.SeasonEpisode)]
    public void In_progress_reads_type_and_progress(string line, MediaType mediaType, string progress, ProgressUnit unit)
    {
        var entry = new InProgressLineParser().Parse(line).Value;

        entry.MediaType.ShouldBe(mediaType);
        entry.Progress.Value.ShouldBe(progress);
        entry.Progress.Unit.ShouldBe(unit);
    }

    [Fact]
    public void In_progress_missing_the_progress_fails()
    {
        var result = new InProgressLineParser().Parse("One Piece___Anime");

        result.Error.ShouldBe(Progress.Empty);
    }

    [Fact]
    public void Queue_reads_the_media_type()
    {
        var entry = new QueueLineParser().Parse("Kotoha no Niwa___Movie").Value;

        entry.Title.Value.ShouldBe("Kotoha no Niwa");
        entry.MediaType.ShouldBe(MediaType.Movie);
    }

    [Fact]
    public void Queue_missing_the_media_type_fails()
    {
        var result = new QueueLineParser().Parse("Shirobako");

        result.Error.ShouldBe(MediaTypeExtensions.Unknown);
    }

    [Fact]
    public void Game_and_manga_take_the_whole_line_as_title()
    {
        new GameLineParser().Parse("Hollow Knight").Value.Title.Value.ShouldBe("Hollow Knight");
        new MangaLineParser().Parse("Koe no Katachi").Value.Title.Value.ShouldBe("Koe no Katachi");
    }

    [Fact]
    public void Extra_fields_fail_instead_of_being_ignored()
    {
        var result = new AnimeLineParser().Parse("Naruto___naruto.jpg___leftover");

        result.Error!.Code.ShouldBe("TextFile.TooManyFields");
    }

    [Fact]
    public void Empty_title_fails()
    {
        var result = new MovieLineParser().Parse("___interstellar.jpg");

        result.Error.ShouldBe(Title.Empty);
    }
}