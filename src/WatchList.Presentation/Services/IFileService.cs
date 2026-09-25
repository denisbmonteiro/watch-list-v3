using WatchList.Presentation.Models;

namespace WatchList.Presentation.Services;

public interface IFileService
{
    Task<List<InProgress>> ReadInProgressFileAsync();

    Task<List<Anime>> ReadAnimeFileAsync();

    Task<List<Book>> ReadBookFileAsync();

    Task<List<Game>> ReadGameFileAsync();

    Task<List<Manga>> ReadMangaFileAsync();

    Task<List<Movie>> ReadMovieFileAsync();

    Task<List<Serie>> ReadSerieFileAsync();

    Task<List<Queue>> ReadQueueFileAsync();
}