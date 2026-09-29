using WatchList.Domain.ValueObjects;

namespace WatchList.Domain.Tests.ValueObjects;

public sealed class ProgressTests
{
    [Theory]
    [InlineData("1176", MediaType.Anime, ProgressUnit.Episode)]
    [InlineData("01", MediaType.Anime, ProgressUnit.Episode)]
    [InlineData("531.6", MediaType.Manga, ProgressUnit.Chapter)]
    [InlineData("531", MediaType.Manga, ProgressUnit.Chapter)]
    [InlineData("200", MediaType.Book, ProgressUnit.Page)]
    [InlineData("S05E16", MediaType.Series, ProgressUnit.SeasonEpisode)]
    [InlineData("1h32", MediaType.Movie, ProgressUnit.None)]
    public void Create_keeps_the_original_text(string raw, MediaType type, ProgressUnit unit)
    {
        var progress = Progress.Create(raw, type).Value;

        progress.Value.ShouldBe(raw);
        progress.Unit.ShouldBe(unit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_rejects_empty(string? raw) =>
        Progress.Create(raw, MediaType.Anime).Error.ShouldBe(Progress.Empty);

    [Theory]
    [InlineData("12.5", MediaType.Anime)]
    [InlineData("-3", MediaType.Anime)]
    [InlineData("twelve", MediaType.Anime)]
    [InlineData("531,6", MediaType.Manga)]
    [InlineData("12.5", MediaType.Book)]
    [InlineData("1176", MediaType.Series)]
    public void Create_rejects_values_that_do_not_fit_the_unit(string raw, MediaType type) =>
        Progress.Create(raw, type).Error.ShouldBe(Progress.Invalid);
}
