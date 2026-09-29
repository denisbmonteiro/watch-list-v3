using WatchList.Application.Common;

namespace WatchList.Application.Tests.Common;

public sealed class PagedResultTests
{
    private static readonly int[] _numbers = [.. Enumerable.Range(1, 30)];

    [Fact]
    public void Returns_the_requested_page()
    {
        var page = _numbers.ToPagedResult(new PageRequest(1, 12), totalCount: 30);

        page.Items.ShouldBe([.. Enumerable.Range(13, 12)]);
        page.PageIndex.ShouldBe(1);
        page.TotalPages.ShouldBe(3);
        page.HasPreviousPage.ShouldBeTrue();
        page.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public void Last_page_can_be_partial()
    {
        var page = _numbers.ToPagedResult(new PageRequest(2, 12), totalCount: 30);

        page.Items.ShouldBe([.. Enumerable.Range(25, 6)]);
        page.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void Page_past_the_end_is_clamped_to_the_last_one()
    {
        var page = _numbers.ToPagedResult(new PageRequest(9, 12), totalCount: 30);

        page.PageIndex.ShouldBe(2);
        page.Items.Count.ShouldBe(6);
    }

    [Fact]
    public void All_puts_everything_on_one_page()
    {
        var page = _numbers.ToPagedResult(PageRequest.All, totalCount: 30);

        page.Items.Count.ShouldBe(30);
        page.TotalPages.ShouldBe(1);
        page.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void Empty_result_still_has_one_page()
    {
        var page = Array.Empty<int>().ToPagedResult(new PageRequest(3, 12), totalCount: 30);

        page.Items.ShouldBeEmpty();
        page.PageIndex.ShouldBe(0);
        page.TotalPages.ShouldBe(1);
        page.FilteredCount.ShouldBe(0);
        page.TotalCount.ShouldBe(30);
    }

    [Theory]
    [InlineData(-1, 12)]
    [InlineData(0, 0)]
    public void Invalid_page_request_throws(int pageIndex, int pageSize) =>
        Should.Throw<ArgumentOutOfRangeException>(() => new PageRequest(pageIndex, pageSize));
}
