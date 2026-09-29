namespace WatchList.Shared.Extensions;

public static class EnumerableExtensions
{
    public static IOrderedEnumerable<T> OrderByIgnoreCase<T>(this IEnumerable<T> source, Func<T, string> keySelector) =>
        source.OrderBy(keySelector, StringComparer.InvariantCultureIgnoreCase);

    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate) =>
        condition ? source.Where(predicate) : source;
}
