using System.Text;

namespace WatchList.Shared.Extensions;

public static class StringExtensions
{
    private static readonly char[] _droppedFromSlug = ['\'', '`', '’', '´'];

    public static bool ContainsIgnoreCase(this string value, string term)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(term);
        return value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Must match <c>slugify</c> in the <c>update-covers</c> skill, which named the cover files on disk.
    /// </summary>
    public static string ToSlug(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var slug = new StringBuilder(value.Length);

        foreach (var c in value.ToLowerInvariant())
        {
            if (Array.IndexOf(_droppedFromSlug, c) >= 0)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(c);
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        return slug.ToString().TrimEnd('-');
    }
}
