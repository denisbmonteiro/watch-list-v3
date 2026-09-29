using WatchList.Shared.Extensions;

namespace WatchList.Shared.Tests.Extensions;

public sealed class EnumerableExtensionsTests
{
    [Fact]
    public void OrderByIgnoreCase_does_not_put_uppercase_first()
    {
        string[] fruits = ["banana", "Cherry", "apple"];

        fruits.OrderByIgnoreCase(x => x).ShouldBe(["apple", "banana", "Cherry"]);
    }

    [Fact]
    public void WhereIf_filters_only_when_asked()
    {
        int[] numbers = [1, 2, 3, 4];

        numbers.WhereIf(true, n => n > 2).ShouldBe([3, 4]);
        numbers.WhereIf(false, n => n > 2).ShouldBe(numbers);
    }
}
