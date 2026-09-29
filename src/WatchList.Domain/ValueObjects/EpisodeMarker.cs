using System.Globalization;
using System.Text.RegularExpressions;
using WatchList.Shared.Results;

namespace WatchList.Domain.ValueObjects;

public sealed partial record EpisodeMarker(int Season, int Episode)
{
    public static readonly Error Invalid = new(
        "EpisodeMarker.Invalid",
        "Episode marker must be in the SxxEyy format (e.g. S05E16).");

    public int Season { get; } = Season >= 0
        ? Season
        : throw new ArgumentOutOfRangeException(nameof(Season), Season, "Season cannot be negative.");

    public int Episode { get; } = Episode >= 0
        ? Episode
        : throw new ArgumentOutOfRangeException(nameof(Episode), Episode, "Episode cannot be negative.");

    public static Result<EpisodeMarker> Parse(string? raw)
    {
        var match = Pattern().Match(raw?.Trim() ?? string.Empty);

        if (!match.Success)
        {
            return Invalid;
        }

        return new EpisodeMarker(
            int.Parse(match.Groups["season"].ValueSpan, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["episode"].ValueSpan, CultureInfo.InvariantCulture));
    }

    public override string ToString() => $"S{Season:00}E{Episode:00}";

    [GeneratedRegex(@"^S(?<season>\d{1,3})E(?<episode>\d{1,4})$", RegexOptions.IgnoreCase)]
    private static partial Regex Pattern();
}
