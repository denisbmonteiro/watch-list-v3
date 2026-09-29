using WatchList.Domain.ValueObjects;

namespace WatchList.Domain.Tests.ValueObjects;

public sealed class EpisodeMarkerTests
{
    [Theory]
    [InlineData("S05E16", 5, 16)]
    [InlineData("s01e06", 1, 6)]
    [InlineData("S11E24", 11, 24)]
    [InlineData("S00E01", 0, 1)]
    public void Parse_reads_season_and_episode(string raw, int season, int episode) =>
        EpisodeMarker.Parse(raw).Value.ShouldBe(new EpisodeMarker(season, episode));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("5x16")]
    [InlineData("S05")]
    [InlineData("S05E")]
    [InlineData("SE16")]
    [InlineData("S05E16 extra")]
    public void Parse_rejects_other_formats(string? raw) =>
        EpisodeMarker.Parse(raw).Error.ShouldBe(EpisodeMarker.Invalid);

    [Theory]
    [InlineData(5, 16, "S05E16")]
    [InlineData(1, 6, "S01E06")]
    [InlineData(12, 100, "S12E100")]
    public void ToString_pads_to_two_digits(int season, int episode, string expected) =>
        new EpisodeMarker(season, episode).ToString().ShouldBe(expected);

    [Fact]
    public void Round_trips_the_file_format() =>
        EpisodeMarker.Parse("S03E13").Value.ToString().ShouldBe("S03E13");

    [Fact]
    public void Rejects_negative_numbers()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new EpisodeMarker(-1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => new EpisodeMarker(1, -1));
    }
}
