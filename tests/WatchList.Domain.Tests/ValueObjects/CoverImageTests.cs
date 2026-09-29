using WatchList.Domain.ValueObjects;

namespace WatchList.Domain.Tests.ValueObjects;

public sealed class CoverImageTests
{
    [Theory]
    [InlineData("86.jpg")]
    [InlineData("breaking-bad.jpg")]
    [InlineData("kaguya-sama-wa-kokurasetai-2.jpg")]
    public void Create_accepts_slug_file_names(string fileName) =>
        CoverImage.Create(fileName).Value.FileName.ShouldBe(fileName);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("breaking-bad")]
    [InlineData("breaking-bad.png")]
    [InlineData("Breaking-Bad.jpg")]
    [InlineData("breaking bad.jpg")]
    [InlineData("-breaking-bad.jpg")]
    [InlineData("breaking--bad.jpg")]
    [InlineData("series/breaking-bad.jpg")]
    [InlineData("../breaking-bad.jpg")]
    public void Create_rejects_what_is_not_a_slug_jpg(string? fileName) =>
        CoverImage.Create(fileName).Error.ShouldBe(CoverImage.Invalid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateOptional_turns_empty_into_null(string? fileName)
    {
        var result = CoverImage.CreateOptional(fileName);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void CreateOptional_still_validates_what_is_filled() =>
        CoverImage.CreateOptional("cover.png").Error.ShouldBe(CoverImage.Invalid);
}
