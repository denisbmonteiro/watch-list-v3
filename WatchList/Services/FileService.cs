using WatchList.Models;

namespace WatchList.Services;

public class FileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<List<InProgress>> ReadInProgressFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "index.txt");
        var list = new List<InProgress>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines.Order())
        {
            var inProgress = new InProgress
            {
                Name = line.Split('_')[0],
                Type = line.Split('_')[1],
                Progress = line.Split('_')[2]
            };

            list.Add(inProgress);
        }

        return list;
    }

    public async Task<List<Anime>> ReadAnimeFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "anime.txt");
        var list = new List<Anime>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines.Order())
        {
            var anime = new Anime
            {
                Name = line
            };

            list.Add(anime);
        }

        return list;
    }

    public async Task<List<Book>> ReadBookFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "books.txt");
        var list = new List<Book>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines)
        {
            var book = new Book
            {
                Title = line.Split('_')[0],
                Author = line.Split('_')[1]
            };

            list.Add(book);
        }

        return list.OrderBy(l => l.Title).ToList();
    }

    public async Task<List<Game>> ReadGameFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "games.txt");
        var list = new List<Game>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines)
        {
            var game = new Game
            {
                Name = line
            };

            list.Add(game);
        }

        return list.OrderBy(l => l.Name).ToList();
    }

    public async Task<List<Manga>> ReadMangaFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "manga.txt");
        var list = new List<Manga>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines)
        {
            var manga = new Manga
            {
                Name = line
            };

            list.Add(manga);
        }

        return list.OrderBy(l => l.Name).ToList();
    }

    public async Task<List<Movie>> ReadMovieFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "movies.txt");
        var list = new List<Movie>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines)
        {
            var movie = new Movie
            {
                Name = line
            };

            list.Add(movie);
        }

        return list.OrderBy(l => l.Name).ToList();
    }

    public async Task<List<Serie>> ReadSerieFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "series.txt");
        var list = new List<Serie>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines)
        {
            var fields = line.Split('_');

            var serie = new Serie
            {
                Name = fields.Length > 0 ? fields[0] : string.Empty,
                Progress = fields.Length > 1 ? fields[1] : string.Empty
            };

            list.Add(serie);
        }

        return list.OrderBy(l => l.Name).ToList();
    }

    public async Task<List<Queue>> ReadQueueFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", "queue.txt");
        var list = new List<Queue>();

        if (!File.Exists(path))
            return list;

        var lines = await File.ReadAllLinesAsync(path);

        foreach (var line in lines)
        {
            var queue = new Queue
            {
                Name = line.Split('_')[0],
                Type = line.Split('_')[1]
            };

            list.Add(queue);
        }

        return list.OrderBy(l => l.Name).ToList();
    }
}