using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Tracking.GetQueue;

public sealed record QueueItemDto(string Title, MediaType MediaType);
