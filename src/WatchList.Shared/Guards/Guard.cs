using System.Runtime.CompilerServices;

namespace WatchList.Shared.Guards;

public static class Guard
{
    public static string NotNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }
}
