using WatchList.Domain.ValueObjects;

namespace WatchList.Domain.Tests.ValueObjects;

public sealed class TitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty(string? value) =>
        Title.Create(value).Error.ShouldBe(Title.Empty);

    [Fact]
    public void Create_trims_the_ends() =>
        Title.Create("  Breaking Bad \t").Value.Value.ShouldBe("Breaking Bad");

    [Fact]
    public void Equality_ignores_case()
    {
        var lower = Title.Create("one piece").Value;
        var upper = Title.Create("One Piece").Value;

        lower.ShouldBe(upper);
        lower.GetHashCode().ShouldBe(upper.GetHashCode());
    }

    [Fact]
    public void Different_titles_are_not_equal() =>
        Title.Create("86").Value.ShouldNotBe(Title.Create("86 (2021)").Value);
}
