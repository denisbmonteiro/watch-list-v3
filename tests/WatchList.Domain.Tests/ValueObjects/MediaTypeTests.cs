using WatchList.Domain.ValueObjects;

namespace WatchList.Domain.Tests.ValueObjects;

public sealed class MediaTypeTests
{
    [Theory]
    [InlineData("Anime", MediaType.Anime)]
    [InlineData("anime", MediaType.Anime)]
    [InlineData(" MOVIE ", MediaType.Movie)]
    [InlineData("Series", MediaType.Series)]
    [InlineData("Book", MediaType.Book)]
    [InlineData("game", MediaType.Game)]
    [InlineData("Manga", MediaType.Manga)]
    public void Parse_ignores_case(string value, MediaType expected) =>
        MediaType.Parse(value).Value.ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Serie")]
    [InlineData("0")]
    [InlineData("Anime, Movie")]
    public void Parse_rejects_unknown(string? value) =>
        MediaType.Parse(value).Error.ShouldBe(MediaTypeExtensions.Unknown);

    [Theory]
    [InlineData(MediaType.Anime, ProgressUnit.Episode)]
    [InlineData(MediaType.Series, ProgressUnit.SeasonEpisode)]
    [InlineData(MediaType.Manga, ProgressUnit.Chapter)]
    [InlineData(MediaType.Book, ProgressUnit.Page)]
    [InlineData(MediaType.Movie, ProgressUnit.None)]
    [InlineData(MediaType.Game, ProgressUnit.None)]
    public void ProgressUnit_comes_from_the_media_type(MediaType type, ProgressUnit expected) =>
        type.ProgressUnit.ShouldBe(expected);

    [Fact]
    public void Every_media_type_has_a_progress_unit()
    {
        foreach (var type in Enum.GetValues<MediaType>())
        {
            Enum.IsDefined(type.ProgressUnit).ShouldBeTrue();
        }
    }
}
