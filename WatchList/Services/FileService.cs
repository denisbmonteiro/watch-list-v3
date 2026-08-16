using WatchList.Models;
using WatchList.Constants;

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
        var lines = await ReadFileAsync(FileNames.Index);
        var list = new List<InProgress>();

        foreach (var line in lines)
        {
            var fields = line.Split(FileHandling.SplitSeparator);

            var inProgress = new InProgress
            {
                Name = fields.Length > 0 ? fields[0] : string.Empty,
                Type = fields.Length > 1 ? fields[1] : string.Empty,
                Progress = fields.Length > 2 ? fields[2] : string.Empty,
                ImageUrl = fields.Length > 3 ? fields[3] : string.Empty
            };

            list.Add(inProgress);
        }

        return list.OrderBy(l => l.Name).ToList();
    }

    public async Task<List<Anime>> ReadAnimeFileAsync()
    {
        var lines = await ReadFileAsync(FileNames.Anime);
        var list = new List<Anime>();

        foreach (var line in lines)
        {
            var fields = line.Split(FileHandling.SplitSeparator);

            var anime = new Anime
            {
                Name = fields.Length > 0 ? fields[0] : string.Empty,
                ImageUrl = fields.Length > 1 ? fields[1] : string.Empty
            };

            list.Add(anime);
        }

        return list.OrderBy(l => l.Name).ToList();
    }

    public async Task<List<Book>> ReadBookFileAsync()
    {
        var lines = await ReadFileAsync(FileNames.Books);
        var list = new List<Book>();

        foreach (var line in lines)
        {
            var fields = line.Split(FileHandling.SplitSeparator);

            var book = new Book
            {
                Title = fields.Length > 0 ? fields[0] : string.Empty,
                Author = fields.Length > 1 ? fields[1] : string.Empty
            };

            list.Add(book);
        }

        return list.OrderBy(l => l.Title).ToList();
    }

    public async Task<List<Game>> ReadGameFileAsync()
    {
        var lines = await ReadFileAsync(FileNames.Games);
        var list = new List<Game>();

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
        var path = Path.Combine(_environment.WebRootPath, "AppData", FileNames.Manga);
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
        var path = Path.Combine(_environment.WebRootPath, "AppData", FileNames.Movies);
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
        var lines = await ReadFileAsync(FileNames.Series);
        var list = new List<Serie>();

        foreach (var line in lines)
        {
            var fields = line.Split(FileHandling.SplitSeparator);

            var serie = new Serie
            {
                Name = fields.Length > 0 ? fields[0] : string.Empty,
                Progress = fields.Length > 1 ? fields[1] : string.Empty,
                ImageUrl = fields.Length > 2 ? fields[2] : string.Empty
            };

            list.Add(serie);
        }

        return list.OrderBy(l => l.Name).ToList();
    }

    public async Task<List<Queue>> ReadQueueFileAsync()
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", FileNames.Queue);
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

    private async Task<string[]> ReadFileAsync(string fileName)
    {
        var path = Path.Combine(_environment.WebRootPath, "AppData", fileName);

        if (!File.Exists(path))
            return [];

        return await File.ReadAllLinesAsync(path);
    }
}