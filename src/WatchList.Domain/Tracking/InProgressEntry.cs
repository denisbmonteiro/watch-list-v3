using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Tracking;

public sealed class InProgressEntry
{
    private InProgressEntry(Title title, MediaType mediaType, Progress progress, CoverImage? coverImage)
    {
        Title = title;
        MediaType = mediaType;
        Progress = progress;
        CoverImage = coverImage;
    }

    public Title Title { get; }

    public MediaType MediaType { get; }

    public Progress Progress { get; }

    public CoverImage? CoverImage { get; }

    public static Result<InProgressEntry> Create(string? title, string? mediaType, string? progress, string? coverImage)
    {
        var validTitle = Title.Create(title);
        if (validTitle.IsFailure)
        {
            return validTitle.Error;
        }

        var validType = MediaType.Parse(mediaType);
        if (validType.IsFailure)
        {
            return validType.Error;
        }

        var validProgress = Progress.Create(progress, validType.Value);
        if (validProgress.IsFailure)
        {
            return validProgress.Error;
        }

        var validCover = CoverImage.CreateOptional(coverImage);
        if (validCover.IsFailure)
        {
            return validCover.Error;
        }

        return new InProgressEntry(validTitle.Value, validType.Value, validProgress.Value, validCover.Value);
    }
}