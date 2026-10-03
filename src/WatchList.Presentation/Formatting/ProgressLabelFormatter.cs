using WatchList.Domain.ValueObjects;

namespace WatchList.Presentation.Formatting;

public static class ProgressLabelFormatter
{
    /// <summary><c>12</c> in episodes becomes <c>Episode 12</c>; markers like <c>S05E16</c> are shown as they are.</summary>
    public static string Format(string progress, ProgressUnit unit) => unit switch
    {
        ProgressUnit.Episode => $"Episode {progress}",
        ProgressUnit.Chapter => $"Chapter {progress}",
        ProgressUnit.Page => $"Page {progress}",
        _ => progress,
    };
}
