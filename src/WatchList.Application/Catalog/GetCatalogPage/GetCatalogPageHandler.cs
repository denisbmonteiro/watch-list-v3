using WatchList.Application.Abstractions;
using WatchList.Application.Common;
using WatchList.Domain.Abstractions;
using WatchList.Domain.Catalog;
using WatchList.Domain.ValueObjects;
using WatchList.Shared.Extensions;

namespace WatchList.Application.Catalog.GetCatalogPage;

internal sealed class GetCatalogPageHandler(
    IReadRepository<Anime> animes,
    IReadRepository<Movie> movies,
    IReadRepository<Series> series,
    IReadRepository<Book> books,
    IReadRepository<Game> games,
    IReadRepository<Manga> mangas,
    ICoverUrlResolver coverUrls) : IQueryHandler<GetCatalogPageQuery, PagedResult<CatalogItemDto>>
{
    public async Task<PagedResult<CatalogItemDto>> HandleAsync(GetCatalogPageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var items = await ListAsync(query.MediaType, cancellationToken);

        List<CatalogItemDto> filtered =
        [
            .. items
                .WhereIf(!string.IsNullOrWhiteSpace(query.Search), item => item.Title.ContainsIgnoreCase(query.Search!))
                .OrderByIgnoreCase(item => item.Title),
        ];

        return filtered.ToPagedResult(query.Page, items.Count);
    }

    private async Task<IReadOnlyList<CatalogItemDto>> ListAsync(MediaType mediaType, CancellationToken cancellationToken) =>
        mediaType switch
        {
            MediaType.Anime => Map(await animes.ListAsync(cancellationToken),
                anime => new(anime.Title.Value, CoverUrl(mediaType, anime.CoverImage))),
            MediaType.Movie => Map(await movies.ListAsync(cancellationToken),
                movie => new(movie.Title.Value, CoverUrl(mediaType, movie.CoverImage))),
            MediaType.Series => Map(await series.ListAsync(cancellationToken),
                show => new(show.Title.Value, CoverUrl(mediaType, show.CoverImage), EpisodeMarker: show.EpisodeMarker.ToString())),
            MediaType.Book => Map(await books.ListAsync(cancellationToken),
                book => new(book.Title.Value, CoverUrl(mediaType, book.CoverImage), Author: book.Author)),
            MediaType.Game => Map(await games.ListAsync(cancellationToken),
                game => new(game.Title.Value, CoverUrl: null)),
            MediaType.Manga => Map(await mangas.ListAsync(cancellationToken),
                manga => new(manga.Title.Value, CoverUrl: null)),
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, null),
        };

    private string? CoverUrl(MediaType mediaType, CoverImage? coverImage) =>
        coverImage is null ? null : coverUrls.Resolve(mediaType, coverImage);

    private static List<CatalogItemDto> Map<T>(IReadOnlyList<T> source, Func<T, CatalogItemDto> toDto) =>
        [.. source.Select(toDto)];
}
