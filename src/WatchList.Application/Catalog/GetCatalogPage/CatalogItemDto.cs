namespace WatchList.Application.Catalog.GetCatalogPage;

/// <param name="CoverUrl">Null while the title has no cover yet.</param>
/// <param name="Author">Books only.</param>
/// <param name="EpisodeMarker">Series only, as <c>S05E16</c>.</param>
public sealed record CatalogItemDto(string Title, string? CoverUrl, string? Author = null, string? EpisodeMarker = null);
