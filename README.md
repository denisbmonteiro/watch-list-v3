# Watch List

A web app to keep track of anime, movies, series, books, games and manga — what has been watched, what is in progress and what comes next.

**Status:** In production

**Summary**
- **Description:** A personal title list built with Blazor, with the data stored in plain `.txt` files and no database.
- **Technologies:** .NET 10, ASP.NET Core Blazor (Interactive Server), ASP.NET Core Web API, MudBlazor, xUnit v3, Shouldly, Docker.
- **Architecture:** Layered Clean Architecture — Domain, Application, Infrastructure, Presentation, Api and a Shared project.

**Features**
- Home page with the titles in progress and how far along each one is (episode, chapter or page).
- One page per category: Anime, Books, Games, Manga, Movies and Series.
- Queue (`/queue`) with what to watch next.
- Dashboard with totals per category and a pie chart.
- Search, pagination and a cover grid with a popup preview.
- In-memory caching of the data files.
- REST API (`/api/...`) with the same lists, for development alongside the Blazor app.

**How to run**
1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. Clone this repository.
3. Run the app:
   ```bash
   dotnet run --project src/WatchList.Presentation
   ```
4. Open `http://localhost:5285` in your browser.

To run the API instead, see the **API** section below.

With Docker:
```bash
docker build -t watchlist .
docker run -p 8080:8080 watchlist
```

**API**

`WatchList.Api` is a second host on top of the same Application and Infrastructure layers. It reads the same files in `data/` as the Blazor app, so both can run side by side:

```bash
dotnet run --project src/WatchList.Api
```

It listens on `http://localhost:5286`. In Development, the OpenAPI document is at `/openapi/v1.json` and Swagger UI at `/swagger`. The API is read-only: every route is a `GET`.

| Route | Returns |
|---|---|
| `/api/anime`, `/api/movies`, `/api/series`, `/api/books`, `/api/games`, `/api/manga` | One page of that list |
| `/api/in-progress` | One page of the titles in progress, as on the home page |
| `/api/queue` | The whole queue, sorted by title and not paged |
| `/api/dashboard` | How many titles each list holds |

Paged routes take three query parameters:
- `search`: matches the title (and the author, for books), ignoring case.
- `pageIndex`: zero-based, default `0`. A page past the end returns the last page.
- `pageSize`: `1`–`100`, default `12`.

They return `items`, `pageIndex`, `pageSize`, `filteredCount`, `totalCount`, `totalPages`, `hasPreviousPage` and `hasNextPage`. A `pageIndex` or `pageSize` out of range returns `400` with a Problem Details body.

```bash
curl "http://localhost:5286/api/movies?search=strong&pageSize=5"
```

Some fields to know:
- Enums (`mediaType`, `progressUnit`) are serialized as strings, such as `"Anime"` or `"SeasonEpisode"`.
- `coverUrl` is a path relative to the Blazor app, such as `images/movie/12-strong.jpg`, because the covers are served by the Blazor app. It is `null` while a title has no cover.
- `author` is filled only for books, and `episodeMarker` only for series.

The Docker image ships only the Blazor app; the API is not deployed.

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
- [src/WatchList.Api](src/WatchList.Api) — REST API with one controller per list.
- [src/WatchList.Shared](src/WatchList.Shared) — `Result`, guards and extensions.
- [data](data) — the `.txt` lists.
- [tests](tests) — unit tests for each layer and integration tests for the API.

**Contributing**
- Suggestions, fixes and improvements are welcome. Open an issue describing the proposal before sending a pull request.

**Author**
- Denis Bittencourt — https://denisbm.com