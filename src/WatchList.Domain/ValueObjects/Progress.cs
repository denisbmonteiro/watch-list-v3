using System.Globalization;
using WatchList.Shared.Results;

namespace WatchList.Domain.ValueObjects;

/// <summary>Keeps the raw text (<c>01</c>, <c>531.6</c>) because that is what gets displayed.</summary>
public sealed record Progress
{
    public static readonly Error Empty = new("Progress.Empty", "Progress cannot be empty.");

    public static readonly Error Invalid = new(
        "Progress.Invalid",
        "Progress does not fit its unit (episodes and pages are integers; chapters may be decimal, e.g. 531.1).");

    private Progress(string value, ProgressUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    public string Value { get; }

    public ProgressUnit Unit { get; }

    public static Result<Progress> Create(string? raw, MediaType mediaType)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Empty;
        }

        var value = raw.Trim();
        var unit = mediaType.ProgressUnit;

        return IsValid(value, unit) ? new Progress(value, unit) : Invalid;
    }

    private static bool IsValid(string value, ProgressUnit unit) => unit switch
    {
        ProgressUnit.Episode or ProgressUnit.Page =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _),
        ProgressUnit.Chapter =>
            decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _),
        ProgressUnit.SeasonEpisode => EpisodeMarker.Parse(value).IsSuccess,
        _ => true,
    };

    public override string ToString() => Value;
}