using WatchList.Domain.ValueObjects;
using WatchList.Shared.Results;

namespace WatchList.Domain.Tracking;

public sealed class QueueEntry
{
    private QueueEntry(Title title, MediaType mediaType)
    {
        Title = title;
        MediaType = mediaType;
    }

    public Title Title { get; }

    public MediaType MediaType { get; }

    public static Result<QueueEntry> Create(string? title, string? mediaType)
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

        return new QueueEntry(validTitle.Value, validType.Value);
    }
}