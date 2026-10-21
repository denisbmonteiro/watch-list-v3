using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Catalog.GetQueue;

public sealed record QueueItemDto(string Title, MediaType MediaType);
