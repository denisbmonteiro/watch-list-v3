using WatchList.Domain.Tracking;
using WatchList.Domain.ValueObjects;

namespace WatchList.Domain.Tests.Tracking;

public sealed class TrackingEntityTests
{
    [Fact]
    public void InProgressEntry_derives_the_unit_from_the_type()
    {
        var entry = InProgressEntry.Create(
            "Tales of Demons and Gods", "Manga", "531.6", "tales-of-demons-and-gods.jpg").Value;

        entry.MediaType.ShouldBe(MediaType.Manga);
        entry.Progress.Value.ShouldBe("531.6");
        entry.Progress.Unit.ShouldBe(ProgressUnit.Chapter);
        entry.CoverImage!.FileName.ShouldBe("tales-of-demons-and-gods.jpg");
    }

    [Fact]
    public void InProgressEntry_with_unknown_type_fails() =>
        InProgressEntry.Create("One Piece", "Cartoon", "1176", null).Error.ShouldBe(MediaTypeExtensions.Unknown);

    [Fact]
    public void InProgressEntry_with_progress_that_does_not_fit_fails() =>
        InProgressEntry.Create("One Piece", "Anime", "S01E01", null).Error.ShouldBe(Progress.Invalid);

    [Fact]
    public void QueueEntry_reads_the_type()
    {
        var entry = QueueEntry.Create("Kotoha no Niwa", "Movie").Value;

        entry.Title.Value.ShouldBe("Kotoha no Niwa");
        entry.MediaType.ShouldBe(MediaType.Movie);
    }

    [Fact]
    public void QueueEntry_without_type_fails() =>
        QueueEntry.Create("Shirobako", null).Error.ShouldBe(MediaTypeExtensions.Unknown);
}
