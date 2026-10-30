# Watch List

A web app to keep track of anime, movies, series, books, games and manga — what has been watched, what is in progress and what comes next.

**Status:** In production

**Summary**
- **Description:** A personal title list built with Blazor, with the data stored in plain `.txt` files and no database.
- **Technologies:** .NET 10, ASP.NET Core Blazor (Interactive Server), MudBlazor, xUnit v3, Shouldly, Docker.
- **Architecture:** Layered Clean Architecture — Domain, Application, Infrastructure, Presentation and a Shared project.

**Features**
- Home page with the titles in progress and how far along each one is (episode, chapter or page).
- One page per category: Anime, Books, Games, Manga, Movies and Series.
- Queue (`/queue`) with what to watch next.
- Dashboard with totals per category and a pie chart.
- Search, pagination and a cover grid with a popup preview.
- In-memory caching of the data files.

**How to run**
1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. Clone this repository.
3. Run the app:
   ```bash
   dotnet run --project src/WatchList.Presentation
   ```
4. Open `http://localhost:5285` in your browser.

With Docker:
```bash
docker build -t watchlist .
docker run -p 8080:8080 watchlist
```

**Tests**
```bash
dotnet test WatchList.slnx
```

The [scripts/verify-docker.sh](scripts/verify-docker.sh) script runs the tests on the Docker SDK image, starts the production image and checks that every page, static file and cover responds correctly.

**Data format**

The lists live in [data](data), at the repository root, one title per line, with fields separated by `___`:

```text
movies.txt   12 Strong___12-strong.jpg
series.txt   Breaking Bad___S05E16___breaking-bad.jpg
index.txt    One Piece___Anime___1180___one-piece.jpg
queue.txt    Shirobako___Anime
books.txt    Overlord 1: The Undead King___Kugane Maruyama
```

Covers live in `wwwroot/images/{anime,book,manga,movie,series}/`. Publishing copies the lists to `AppData/` next to the app, which is what `Storage:DataPath` in [appsettings.json](src/WatchList.Presentation/appsettings.json) points to; in Development, [appsettings.Development.json](src/WatchList.Presentation/appsettings.Development.json) reads `data/` directly, so edits show up without a rebuild.

**Project structure**
- [src/WatchList.Domain](src/WatchList.Domain) — catalog entities and value objects (title, progress, cover).
- [src/WatchList.Application](src/WatchList.Application) — queries (catalog, in progress, queue, dashboard).
- [src/WatchList.Infrastructure](src/WatchList.Infrastructure) — reading and parsing the `.txt` files, caching and cover resolution.
- [src/WatchList.Presentation](src/WatchList.Presentation) — Blazor UI with MudBlazor.
- [src/WatchList.Shared](src/WatchList.Shared) — `Result`, guards and extensions.
- [data](data) — the `.txt` lists.
- [tests](tests) — unit tests for each layer.

**Contributing**
- Suggestions, fixes and improvements are welcome. Open an issue describing the proposal before sending a pull request.

**Author**
- Denis Bittencourt — https://denisbm.com