using WatchList.Shared.Extensions;

namespace WatchList.Shared.Tests.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("Breaking Bad", "breaking-bad")]
    [InlineData("86 (2021)", "86-2021")]
    [InlineData("Marvel's Daredevil", "marvels-daredevil")]
    [InlineData("Borderlands: The Pre-Sequel", "borderlands-the-pre-sequel")]
    [InlineData("Kaguya-sama wa Kokurasetai?", "kaguya-sama-wa-kokurasetai")]
    [InlineData("  --Pokémon--  ", "pok-mon")]
    [InlineData("Don’t Toy with Me, Miss Nagatoro", "dont-toy-with-me-miss-nagatoro")]
    [InlineData("!!!", "")]
    public void ToSlug_follows_the_update_covers_rule(string value, string expected) =>
        value.ToSlug().ShouldBe(expected);

    [Theory]
    [InlineData("One Piece", "piece", true)]
    [InlineData("One Piece", "PIECE", true)]
    [InlineData("One Piece", "naruto", false)]
    public void ContainsIgnoreCase(string value, string term, bool expected) =>
        value.ContainsIgnoreCase(term).ShouldBe(expected);
}
