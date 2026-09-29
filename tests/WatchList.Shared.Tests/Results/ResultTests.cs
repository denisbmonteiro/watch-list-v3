using WatchList.Shared.Results;

namespace WatchList.Shared.Tests.Results;

public sealed class ResultTests
{
    private static readonly Error _failure = new("Test.Failure", "Something failed.");

    [Fact]
    public void Success_carries_the_value()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_carries_the_error_and_has_no_value()
    {
        Result<int> result = _failure;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_failure);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Error_requires_code_and_message()
    {
        Should.Throw<ArgumentException>(() => new Error("", "message"));
        Should.Throw<ArgumentException>(() => new Error("Code", " "));
    }
}
