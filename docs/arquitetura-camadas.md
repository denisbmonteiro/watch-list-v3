# Plano de separação em camadas — WatchList

> Status: **em andamento** (fases 0 a 5 concluídas) · Data: 23/09/2026 · Alvo: .NET 10 / C# 14

Este documento planeja a separação do projeto único `WatchList` em projetos distintos dentro da
mesma solution, seguindo Clean Architecture, DDD (na medida certa para o tamanho do domínio),
Clean Code e as convenções atuais de C#/.NET.

---

## 1. Diagnóstico do estado atual

Hoje tudo vive em `WatchList/WatchList.csproj` (Blazor Server + MudBlazor):

| Área | Onde está | Problema |
|---|---|---|
| Modelos (`Anime`, `Movie`, `Serie`, `Book`, `Game`, `Manga`, `InProgress`, `Queue`) | `Models/` | Classes anêmicas com setters públicos, `string` para tudo (tipo, progresso, capa). `Queue` colide com `System.Collections.Generic.Queue<T>`; `Serie` é um singular incorreto. |
| Leitura dos `.txt` | `Services/FileService.cs` | 8 métodos quase idênticos (split por `___`, montar objeto, ordenar). Mistura I/O, parsing e regra de ordenação. Depende de `IWebHostEnvironment` (ASP.NET), então não pode ser usado fora da web. |
| Constantes | `Constants/FileNames.cs`, `Constants/FileHandling.cs` | Detalhes de armazenamento expostos ao projeto inteiro. |
| Regras nas páginas | `Components/Pages/*.razor` | Busca, paginação, montagem do caminho da imagem (`images/{type}/{file}`), rótulo de progresso ("Episode", "Chapter", "Page") e cor por tipo estão duplicados em até 6 páginas. |
| Dashboard | `DashboardPage.razor` | Faz 8 leituras de arquivo e indexa `data[0]`, `data[5]` por posição mágica. |
| Dados | `wwwroot/AppData/*.txt` | Por estar em `wwwroot`, os `.txt` são servidos publicamente como arquivos estáticos (`GET /AppData/movies.txt`). |

O domínio é **pequeno e somente leitura**. Isso orienta o plano: separar responsabilidades de
verdade, sem cerimônia que não se paga (sem CQRS com barramento, sem event sourcing, sem
repositório por entidade quando um genérico resolve).

---

## 2. Visão da arquitetura proposta

```mermaid
flowchart TB
    P[WatchList.Presentation<br/>Blazor Server + MudBlazor<br/><i>composition root</i>]
    A[WatchList.Application<br/>casos de uso, DTOs, paginação]
    D[WatchList.Domain<br/>entidades, value objects, contratos]
    I[WatchList.Infrastructure<br/>arquivos .txt, parsers, cache, capas]
    S[WatchList.Shared<br/>utilitários sem dependências]

    P --> A
    P -. só no Program.cs .-> I
    I --> A
    A --> D
    D --> S
    A --> S
    I --> S
```

### Regra de dependência

| Projeto | Pode referenciar | Não pode referenciar |
|---|---|---|
| `WatchList.Shared` | nada (só BCL) | qualquer outro projeto, ASP.NET, MudBlazor |
| `WatchList.Domain` | `Shared` | Application, Infrastructure, Presentation, ASP.NET |
| `WatchList.Application` | `Domain`, `Shared`, `Microsoft.Extensions.DependencyInjection.Abstractions` | Infrastructure, Presentation, ASP.NET, `System.IO` para dados |
| `WatchList.Infrastructure` | `Application`, `Domain`, `Shared`, `Microsoft.Extensions.*` (Options, Caching, FileProviders) | Presentation, MudBlazor, `IWebHostEnvironment` |
| `WatchList.Presentation` | `Application`, `Shared`; `Infrastructure` **apenas** para registrar DI no `Program.cs` | — |

Essas regras são verificadas automaticamente por testes de arquitetura (seção 7).

---

## 3. Responsabilidade de cada projeto

### 3.1 `WatchList.Shared` — utilitários transversais

Código genérico, sem conhecimento do negócio, reaproveitável em qualquer camada.

- `Guards/Guard.cs` — validações de argumento (`Guard.NotNullOrWhiteSpace(value)`), lançando
  exceções padronizadas. Use `ArgumentException.ThrowIfNullOrWhiteSpace` da BCL onde bastar.
- `Results/Result.cs`, `Results/Error.cs` — retorno de sucesso/falha para parsing e casos de uso
  sem usar exceção como fluxo de controle.
- `Extensions/StringExtensions.cs` — `ContainsIgnoreCase`, `ToSlug` (a mesma regra de slug que a
  skill `update-covers` usa para nome de capa).
- `Extensions/EnumerableExtensions.cs` — `OrderByIgnoreCase`, `WhereIf`.

> **Cuidado:** projeto "Shared/Common" tende a virar depósito. Regra: se o código cita um conceito
> do WatchList (título, capa, anime), ele **não** é Shared — vai para Domain ou Application.

### 3.2 `WatchList.Domain` — regras de negócio

O coração do sistema. Sem I/O, sem framework, 100% testável.

**Entidades** (imutáveis, criadas por factory com validação; o título é a identidade natural):

| Atual | Proposto | Observação |
|---|---|---|
| `Anime` | `Anime(Title, CoverImage?)` | |
| `Movie` | `Movie(Title, CoverImage?)` | |
| `Serie` | `Series(Title, EpisodeMarker, CoverImage?)` | corrige o nome; `S05E16` vira value object |
| `Book` | `Book(Title, Author, CoverImage?)` | |
| `Game` | `Game(Title)` | |
| `Manga` | `Manga(Title)` | |
| `InProgress` | `InProgressEntry(Title, MediaType, Progress, CoverImage?)` | contexto "Tracking" |
| `Queue` | `QueueEntry(Title, MediaType)` | elimina a colisão com `Queue<T>` |

**Value objects** (`sealed record` — igualdade por valor de graça, sem classe base):

- `Title` — não vazio, sem espaços nas pontas; comparação ordinal-ignore-case.
- `CoverImage` — nome de arquivo `.jpg` válido (o slug). Não conhece URL nem pasta.
- `MediaType` — enum `Anime, Movie, Series, Book, Game, Manga` com `MediaType.Parse("Anime")`
  tolerante a caixa (hoje é `string` comparada com `ToLower()` nas páginas). `Parse` e
  `ProgressUnit` são membros de extensão do C# 14 (`MediaTypeExtensions`), já que enum não tem
  método próprio.
- `Progress` — valor bruto + `ProgressUnit` (`Episode`, `Chapter`, `Page`, `SeasonEpisode`, e
  `None` para filme e jogo) derivado do `MediaType`. Guarda o texto original porque há capítulos
  como `531.1`.
- `EpisodeMarker` — parse de `S05E16` em `Season`/`Episode`.

**Contratos:**

- `Abstractions/IReadRepository<T>` — `Task<IReadOnlyList<T>> ListAsync(CancellationToken ct)`.
  Um genérico substitui os 8 métodos do `IFileService`.

A regra "qual unidade de progresso cada tipo usa" fica aqui. O **texto** exibido ("Episode 12")
é apresentação e fica na Presentation.

### 3.3 `WatchList.Application` — casos de uso

Orquestra o domínio para atender a UI. Não sabe que os dados vêm de `.txt`.

- **Consultas** (um handler por caso de uso, sem MediatR — a biblioteca ficou comercial e aqui
  não se paga):
  - `Catalog/GetCatalogPage` — `GetCatalogPageQuery(MediaType, Search, PageRequest)` →
    `PagedResult<CatalogItemDto>`. Concentra busca por título, ordenação e paginação que hoje
    estão copiadas em 6 páginas.
  - `Tracking/GetInProgressPage` e `Tracking/GetQueue`.
  - `Dashboard/GetDashboardSummary` → `DashboardSummaryDto` com contagem por categoria nomeada
    (fim do `data[5]`).
- **Contratos implementados fora:** `ICoverUrlResolver` (transforma `MediaType` + `CoverImage`
  em URL; quem sabe que é `images/series/x.jpg` é a Infrastructure).
- **Comum:** `PageRequest`, `PagedResult<T>`, interface `IQueryHandler<TQuery, TResult>`.
- `DependencyInjection.cs` — `services.AddApplication()`.

### 3.4 `WatchList.Infrastructure` — detalhes técnicos

Implementa os contratos do Domain/Application.

- `Persistence/TextFiles/TextFileReader.cs` — lê as linhas via `IFileProvider` (não mais
  `IWebHostEnvironment`), trata BOM (o `queue.txt` começa com `﻿`) e `\r`.
- `Persistence/TextFiles/TextFileRepository<T>` — implementação única de `IReadRepository<T>`.
- `Persistence/TextFiles/Parsers/ILineParser<T>` + um parser por formato
  (`AnimeLineParser`, `SeriesLineParser`, …). Cada parser é pequeno e testável isoladamente;
  linha inválida gera `Result` com erro e é registrada em log em vez de virar objeto vazio.
- `Persistence/TextFiles/TextFileLayout.cs` — separador `___` e nome do arquivo por entidade
  (substitui `Constants/`).
- `Covers/CoverUrlResolver.cs` — mapeia `MediaType` → pasta (`Series` → `series`, `Book` →
  `book`…).
- `Caching/CachedReadRepository<T>` — decorator com `IMemoryCache` + change token do arquivo:
  os `.txt` só são relidos quando mudam (hoje o Dashboard lê os 8 arquivos a cada visita).
- `Options/StorageOptions.cs` — `Storage:DataPath` no `appsettings.json`.
- `DependencyInjection.cs` — `services.AddInfrastructure(configuration)`.

### 3.5 `WatchList.Presentation` — Blazor

Só UI e composição.

- `Program.cs` — composition root: `AddApplication()`, `AddInfrastructure(...)`,
  `AddMudServices()`.
- **Componentes compartilhados** extraídos da duplicação atual:
  - `CoverGrid<TItem>` — grade + card + placeholder (Home, Anime, Movie, Series, Book, Manga).
  - `CoverPreviewDialog` — o lightbox.
  - `SearchBox`, `ListPager` — busca e paginação (UI; a lógica fica na Application).
  - `TitleTable` — tabela simples usada em Game, Manga e Queue.
- `Formatting/ProgressLabelFormatter.cs` — `ProgressUnit.Episode` → "Episode 12".
- `Formatting/MediaTypeColors.cs` — `MediaType` → `MudBlazor.Color`.
- As páginas passam a ter só estado de tela e uma chamada a handler.

---

## 4. Estrutura final de pastas e projetos

```
watch-list-v3/
├── .claude/
│   └── skills/update-covers/                 # caminhos atualizados (ver seção 6)
├── docs/
│   └── arquitetura-camadas.md
├── scripts/
│   └── verify-docker.sh                      # verificação de fim de fase (seção 6)
├── src/
│   ├── WatchList.Shared/
│   │   ├── WatchList.Shared.csproj
│   │   ├── Extensions/
│   │   │   ├── EnumerableExtensions.cs
│   │   │   └── StringExtensions.cs
│   │   ├── Guards/
│   │   │   └── Guard.cs
│   │   └── Results/
│   │       ├── Error.cs
│   │       └── Result.cs
│   │
│   ├── WatchList.Domain/
│   │   ├── WatchList.Domain.csproj
│   │   ├── Abstractions/
│   │   │   └── IReadRepository.cs
│   │   ├── Catalog/
│   │   │   ├── Anime.cs
│   │   │   ├── Book.cs
│   │   │   ├── Game.cs
│   │   │   ├── Manga.cs
│   │   │   ├── Movie.cs
│   │   │   └── Series.cs
│   │   ├── Tracking/
│   │   │   ├── InProgressEntry.cs
│   │   │   └── QueueEntry.cs
│   │   └── ValueObjects/
│   │       ├── CoverImage.cs
│   │       ├── EpisodeMarker.cs
│   │       ├── MediaType.cs
│   │       ├── Progress.cs
│   │       ├── ProgressUnit.cs
│   │       └── Title.cs
│   │
│   ├── WatchList.Application/
│   │   ├── WatchList.Application.csproj
│   │   ├── DependencyInjection.cs
│   │   ├── Abstractions/
│   │   │   ├── ICoverUrlResolver.cs
│   │   │   └── IQueryHandler.cs
│   │   ├── Common/
│   │   │   ├── PagedResult.cs
│   │   │   └── PageRequest.cs
│   │   ├── Catalog/
│   │   │   └── GetCatalogPage/
│   │   │       ├── CatalogItemDto.cs
│   │   │       ├── GetCatalogPageHandler.cs
│   │   │       └── GetCatalogPageQuery.cs
│   │   ├── Tracking/
│   │   │   ├── GetInProgressPage/
│   │   │   │   ├── GetInProgressPageHandler.cs
│   │   │   │   ├── GetInProgressPageQuery.cs
│   │   │   │   └── InProgressItemDto.cs
│   │   │   └── GetQueue/
│   │   │       ├── GetQueueHandler.cs
│   │   │       ├── GetQueueQuery.cs
│   │   │       └── QueueItemDto.cs
│   │   └── Dashboard/
│   │       └── GetDashboardSummary/
│   │           ├── DashboardSummaryDto.cs
│   │           ├── GetDashboardSummaryHandler.cs
│   │           └── GetDashboardSummaryQuery.cs
│   │
│   ├── WatchList.Infrastructure/
│   │   ├── WatchList.Infrastructure.csproj
│   │   ├── DependencyInjection.cs
│   │   ├── Caching/
│   │   │   └── CachedReadRepository.cs
│   │   ├── Covers/
│   │   │   └── CoverUrlResolver.cs
│   │   ├── Options/
│   │   │   └── StorageOptions.cs
│   │   └── Persistence/
│   │       └── TextFiles/
│   │           ├── TextFileLayout.cs
│   │           ├── TextFileReader.cs
│   │           ├── TextFileRepository.cs
│   │           └── Parsers/
│   │               ├── AnimeLineParser.cs
│   │               ├── BookLineParser.cs
│   │               ├── GameLineParser.cs
│   │               ├── ILineParser.cs
│   │               ├── InProgressLineParser.cs
│   │               ├── MangaLineParser.cs
│   │               ├── MovieLineParser.cs
│   │               ├── QueueLineParser.cs
│   │               └── SeriesLineParser.cs
│   │
│   └── WatchList.Presentation/
│       ├── WatchList.Presentation.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── AppData/                           # .txt saem do wwwroot (não ficam públicos)
│       │   ├── anime.txt
│       │   ├── books.txt
│       │   ├── games.txt
│       │   ├── index.txt
│       │   ├── manga.txt
│       │   ├── movies.txt
│       │   ├── queue.txt
│       │   └── series.txt
│       ├── Components/
│       │   ├── _Imports.razor
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   └── NavMenu.razor
│       │   ├── Pages/
│       │   │   ├── AnimePage.razor
│       │   │   ├── BookPage.razor
│       │   │   ├── DashboardPage.razor
│       │   │   ├── ErrorPage.razor
│       │   │   ├── GamePage.razor
│       │   │   ├── HomePage.razor
│       │   │   ├── MangaPage.razor
│       │   │   ├── MoviePage.razor
│       │   │   ├── QueuePage.razor
│       │   │   └── SeriesPage.razor
│       │   └── Shared/
│       │       ├── CoverGrid.razor
│       │       ├── CoverPreviewDialog.razor
│       │       ├── ListPager.razor
│       │       ├── SearchBox.razor
│       │       └── TitleTable.razor
│       ├── Formatting/
│       │   ├── MediaTypeColors.cs
│       │   └── ProgressLabelFormatter.cs
│       └── wwwroot/
│           ├── app.css
│           ├── favicon.ico
│           └── images/
│               ├── anime/
│               ├── book/
│               ├── manga/
│               ├── movie/
│               └── series/
│
├── tests/
│   ├── WatchList.Shared.Tests/
│   │   ├── WatchList.Shared.Tests.csproj
│   │   ├── Extensions/                        # ToSlug igual ao slugify da skill
│   │   └── Results/
│   ├── WatchList.Domain.Tests/
│   │   ├── WatchList.Domain.Tests.csproj
│   │   ├── Catalog/
│   │   ├── Tracking/
│   │   └── ValueObjects/                      # Title, EpisodeMarker, Progress, MediaType
│   ├── WatchList.Application.Tests/
│   │   ├── WatchList.Application.Tests.csproj
│   │   ├── Catalog/                           # busca, ordenação, paginação
│   │   └── Dashboard/
│   ├── WatchList.Infrastructure.Tests/
│   │   ├── WatchList.Infrastructure.Tests.csproj
│   │   ├── Fixtures/                          # .txt de exemplo (com BOM, \r, linha quebrada)
│   │   └── Parsers/
│   └── WatchList.ArchitectureTests/
│       ├── WatchList.ArchitectureTests.csproj
│       └── LayerDependencyTests.cs
│
├── .dockerignore
├── .editorconfig
├── .gitignore
├── Directory.Build.props
├── Directory.Packages.props
├── Dockerfile
├── global.json
├── README.md
└── WatchList.slnx
```

**Pastas de solution** no `WatchList.slnx`: `src/` e `tests/`, espelhando o disco.

---

## 5. Arquivos de apoio e exemplos

### 5.1 `Directory.Build.props` (raiz) — configuração comum

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### 5.2 `Directory.Packages.props` — versões centralizadas

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MudBlazor" Version="9.9.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.*" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.*" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="10.0.*" />
    <PackageVersion Include="Microsoft.Extensions.FileProviders.Physical" Version="10.0.*" />
    <!-- testes (xunit.v3 roda no Microsoft.Testing.Platform, ligado no global.json) -->
    <PackageVersion Include="xunit.v3" Version="4.0.1" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="NetArchTest.Rules" Version="..." />
  </ItemGroup>
</Project>
```

Fixe as versões exatas no momento da implementação (sem curinga em produção).

### 5.3 Referências entre projetos

```xml
<!-- WatchList.Domain.csproj -->
<ProjectReference Include="..\WatchList.Shared\WatchList.Shared.csproj" />

<!-- WatchList.Application.csproj -->
<ProjectReference Include="..\WatchList.Domain\WatchList.Domain.csproj" />

<!-- WatchList.Infrastructure.csproj -->
<ProjectReference Include="..\WatchList.Application\WatchList.Application.csproj" />

<!-- WatchList.Presentation.csproj (Sdk="Microsoft.NET.Sdk.Web") -->
<ProjectReference Include="..\WatchList.Application\WatchList.Application.csproj" />
<ProjectReference Include="..\WatchList.Infrastructure\WatchList.Infrastructure.csproj" />
```

### 5.4 Value object no Domain

```csharp
namespace WatchList.Domain.ValueObjects;

public sealed record EpisodeMarker(int Season, int Episode)
{
    public static Result<EpisodeMarker> Parse(string raw) { /* "S05E16" */ }

    public override string ToString() => $"S{Season:00}E{Episode:00}";
}
```

### 5.5 Repositório único + parser na Infrastructure

```csharp
internal sealed class TextFileRepository<T>(
    ITextFileReader reader,
    ILineParser<T> parser,
    ILogger<TextFileRepository<T>> logger) : IReadRepository<T>
{
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct)
    {
        var lines = await reader.ReadLinesAsync(parser.FileName, ct);

        return [.. lines
            .Select(parser.Parse)
            .Where(result => LogIfFailure(result))
            .Select(result => result.Value)];
    }
}

internal sealed class SeriesLineParser : ILineParser<Series>
{
    public string FileName => "series.txt";

    public Result<Series> Parse(string line)
    {
        var fields = line.Split(TextFileLayout.Separator);
        // fields: Nome___S05E16___slug.jpg
    }
}
```

### 5.6 Registro de DI

```csharp
// WatchList.Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
    services.AddMemoryCache();
    services.AddSingleton<ITextFileReader, TextFileReader>();
    services.AddSingleton<ILineParser<Anime>, AnimeLineParser>();
    // ... demais parsers
    services.AddSingleton(typeof(IReadRepository<>), typeof(TextFileRepository<>));
    services.Decorate(typeof(IReadRepository<>), typeof(CachedReadRepository<>)); // ou registro manual
    services.AddSingleton<ICoverUrlResolver, CoverUrlResolver>();
    return services;
}

// WatchList.Presentation/Program.cs
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddMudServices();
```

`Decorate` é do Scrutor; sem ele, registre o decorator com uma factory manual — são 8 tipos.

### 5.7 Página depois da refatoração

```razor
@page "/movie"
@inject IQueryHandler<GetCatalogPageQuery, PagedResult<CatalogItemDto>> GetCatalogPage

<h1>Movies</h1>

<CoverGrid Load="LoadAsync" PlaceholderIcon="@Icons.Material.Filled.Tv" />

@code {
    private Task<PagedResult<CatalogItemDto>> LoadAsync(string search, PageRequest page, CancellationToken ct) =>
        GetCatalogPage.HandleAsync(new(MediaType.Movie, search, page), ct);
}
```

As ~175 linhas de cada página de capa viram ~15; a lógica fica testada na Application.

---

## 6. Plano de migração em fases

Cada fase termina com a aplicação **compilando e rodando igual** — dá para parar em qualquer uma.

**Verificação obrigatória no fim de cada fase:** antes do commit/PR, rode

```bash
scripts/verify-docker.sh        # porta opcional: scripts/verify-docker.sh 8090
```

O script falha na primeira divergência e faz, tudo em Docker (o que roda em produção):

1. `dotnet test` da solution inteira dentro da imagem `mcr.microsoft.com/dotnet/sdk:10.0` — a
   mesma do build de produção (SDK, cultura e sistema de arquivos Linux).
2. `docker build` com o `Dockerfile` real — pega `.csproj` novo que não foi copiado antes do
   `restore`, arquivo barrado pelo `.dockerignore` etc.
3. Sobe o container e confere: as 9 páginas respondem 200 com o `<h1>` certo (não a tela de erro),
   `blazor.web.js`, `app.css`, `favicon.ico` e uma capa real são servidos, e o log não tem
   `fail:`/`crit:` nem exceção não tratada.

Fase só é marcada como concluída com o script passando. Se a fase mudar rotas, arquivos estáticos
ou o local dos dados (fases 4 e 5), atualize o script na mesma branch.

**Branches:** cada fase é implementada numa branch local própria, criada a partir da `main`, com o
nome `refactor/fase-<n>` (`refactor/fase-0`, `refactor/fase-1`, …). Sem sufixo descritivo.

| Fase | Branch | Status |
|---|---|---|
| 0 — Preparação da solution | `refactor/fase-0` | ✅ concluída (24/09/2026) |
| 1 — Shared e Domain | `refactor/fase-1` | ✅ concluída (29/09/2026) |
| 2 — Application | `refactor/fase-2` | ✅ concluída (29/09/2026) |
| 3 — Infrastructure | `refactor/fase-3` | ✅ concluída (02/10/2026) |
| 4 — Presentation | `refactor/fase-4` | ✅ concluída (03/10/2026) |
| 5 — Dados fora do `wwwroot` | `refactor/fase-5` | ✅ concluída (07/10/2026) |
| 6 — Guarda-corpos | `refactor/fase-6` | pendente |

### Fase 0 — Preparação da solution ✅ concluída
1. Criar `global.json` (SDK 10.0.x), `Directory.Build.props`, `Directory.Packages.props`,
   `.editorconfig`.
2. Migrar `WatchList.sln` para `WatchList.slnx` (`dotnet sln migrate`).
3. Mover `WatchList/` para `src/WatchList.Presentation/` e renomear o `.csproj` com `git mv`
   (preserva histórico). Ajustar `RootNamespace` e os `@using` do `_Imports.razor`.
4. Atualizar o `Dockerfile`: copiar todos os `.csproj` + `Directory.*.props` antes do
   `dotnet restore` (mantém o cache de camadas) e trocar o `ENTRYPOINT` para
   `WatchList.Presentation.dll`.
5. Atualizar a skill `.claude/skills/update-covers/` (`SKILL.md` e `scripts/tmdb-covers.py`):
   `WatchList/wwwroot/` → `src/WatchList.Presentation/wwwroot/`.

> **Notas da implementação:**
> - O `CA1711` (nome `Queue`) foi suprimido só na classe `Queue`, com justificativa; a supressão
>   sai quando a fase 1 renomear para `QueueEntry`.
> - O `Directory.Packages.props` tem só o MudBlazor; os demais pacotes entram, com versão exata,
>   na fase que os usar.
> - O `.editorconfig` saiu do `.dockerignore` para o build no Docker usar as mesmas regras.
> - Validação: o HTML das 10 páginas, os status HTTP e os arquivos estáticos ficaram iguais aos
>   de antes da mudança (desconsiderando tokens e IDs aleatórios).

### Fase 1 — Shared e Domain ✅ concluída
1. Criar `WatchList.Shared` (Guard, Result, extensões).
2. Criar `WatchList.Domain` com value objects e entidades; renomear `Serie` → `Series` e
   `Queue` → `QueueEntry`.
3. Escrever `WatchList.Domain.Tests` junto (value objects são o melhor retorno de teste aqui).

> **Notas da implementação:**
> - Os projetos novos ainda não são referenciados pela Presentation: os modelos antigos
>   (`Models/`, com `Serie` e `Queue`) continuam lá até a fase 4 migrar as páginas. A supressão do
>   `CA1711` na classe `Queue` foi mantida, com a justificativa apontando para a fase 4.
> - Entidades são `sealed class` sem setters, com construtor privado e factory
>   `Create(...)` que recebe o texto cru do arquivo e devolve `Result<T>` — é o que os parsers da
>   fase 3 vão chamar. Capa vazia vira `null`; capa preenchida fora do padrão `slug.jpg` é erro.
> - `IReadRepository<T>` ficou para a fase 2, junto com quem o consome.
> - `OrderByTitle` virou `OrderByIgnoreCase(keySelector)`: com "Title" no nome ele citaria um
>   conceito do domínio, o que a regra do Shared proíbe.
> - Entrou um `WatchList.Shared.Tests`, que não estava no plano, para travar o `ToSlug` na mesma
>   regra do `slugify` da skill `update-covers` (conferido também nos 749 títulos reais).
> - Todas as linhas reais dos 8 `.txt` passam pelas factories do Domain sem erro.
> - `CA1716` desligado no `.editorconfig` (só olha palavras reservadas do VB, como `Shared` e
>   `Error`); `CA1707` desligado só em `tests/` para nomes `Metodo_faz_tal_coisa`.
> - xunit.v3 4.x só roda no Microsoft.Testing.Platform no SDK 10: o `global.json` ganhou
>   `"test": { "runner": "Microsoft.Testing.Platform" }` e não há `Microsoft.NET.Test.Sdk` nem
>   `xunit.runner.visualstudio`.
> - O `Dockerfile` passou a restaurar só o `.csproj` da Presentation (a `.slnx` agora inclui os
>   testes) e já copia os `.csproj` de Shared e Domain; `tests/` entrou no `.dockerignore`.

### Fase 2 — Application ✅ concluída
1. Criar `IReadRepository<T>` (em `WatchList.Domain/Abstractions/`), `ICoverUrlResolver`,
   `PagedResult<T>`, `PageRequest`.
2. Implementar os handlers: catálogo, em andamento, fila, dashboard.
3. Testes com repositório fake em memória.

> **Notas da implementação:**
> - A Presentation ainda não referencia a Application; os handlers só entram em uso na fase 4
>   (os repositórios e o `ICoverUrlResolver` vêm da fase 3). O `Dockerfile` já copia o `.csproj`.
> - Handlers são `internal sealed` e só são vistos pela interface `IQueryHandler<,>`;
>   `AddApplication()` os registra como scoped. O projeto de testes enxerga os internos via
>   `InternalsVisibleTo`.
> - `PageRequest(PageIndex, PageSize)` é zero-based e valida os limites; `PageSize = int.MaxValue`
>   (`PageRequest.All`) é o "All" do seletor. Página além do fim é ajustada para a última, como o
>   `CurrentPage` das páginas faz hoje.
> - `PagedResult<T>` traz `FilteredCount` (resultado da busca, usado no total de páginas) e
>   `TotalCount` (lista inteira, usado no "Showing X of Y records"), preservando o comportamento
>   atual. `TotalPages` nunca é zero.
> - `CatalogItemDto(Title, CoverUrl, Author, EpisodeMarker)`: um DTO só para os seis tipos; `Author`
>   só em livro, `EpisodeMarker` só em série, `CoverUrl` nulo quando não há capa (Game e Manga
>   sempre).
> - `InProgressItemDto` leva `MediaType` e `ProgressUnit`; o texto ("Episode 12") e a cor ficam
>   para a Presentation.
> - `GetQueue` devolve a fila inteira ordenada, sem busca nem paginação: a `TitleTable` continua
>   filtrando e paginando no cliente e numerando as linhas pela posição na lista completa. Game e
>   Manga fazem o mesmo usando `GetCatalogPage` com `PageRequest.All`.
> - A ordenação passou de `OrderBy(Name)` (cultura corrente) para `OrderByIgnoreCase`
>   (invariante, ignorando caixa) — só muda o desempate entre títulos que diferem na caixa.
> - A busca não faz `Trim` no termo, igual às páginas de hoje; termo em branco devolve tudo.
> - Entrou o `scripts/verify-docker.sh` (testes no SDK do Docker + imagem de produção no ar),
>   agora obrigatório no fim de toda fase.

### Fase 3 — Infrastructure ✅ concluída
1. `TextFileReader` com `IFileProvider` + `StorageOptions`.
2. Um `ILineParser<T>` por arquivo; `TextFileRepository<T>` genérico.
3. `CoverUrlResolver` e `CachedReadRepository<T>`.
4. Testes de parser com fixtures reais (BOM, `\r\n`, campo faltando, capítulo `531.1`).
5. Remover `Services/FileService.cs`, `IFileService.cs` e `Constants/`. → **feito na fase 4**
   (ver notas).

> **Notas da implementação:**
> - O passo 5 ficou para a fase 4: todas as páginas ainda injetam o `IFileService`, e apagá-lo agora
>   quebraria a regra de cada fase terminar rodando igual. Sai junto com `Models/`, quando as
>   páginas passarem para os handlers.
> - A Presentation já referencia Application e Infrastructure e o `Program.cs` chama
>   `AddApplication().AddInfrastructure(...)`; nenhuma página consome os handlers ainda. O
>   `Storage:DataPath` aponta para `wwwroot/AppData` até a fase 5.
> - `DataPath` relativo é resolvido a partir do `ContentRootPath` do `IHostEnvironment`
>   (`Microsoft.Extensions.Hosting.Abstractions`, não ASP.NET); a opção é validada no start.
> - O `IFileProvider` é um `PhysicalFileProvider` registrado como serviço *keyed* (`"Storage"`),
>   para não virar o `IFileProvider` global do container.
> - O `TextFileReader` devolve todas as linhas, inclusive as em branco, para o número da linha no
>   log bater com o arquivo; quem pula as em branco é o `TextFileRepository<T>`. Arquivo ausente
>   vira lista vazia com *warning* (antes era silencioso).
> - Linha inválida é descartada com *warning* estruturado (`Skipping line 3 of series.txt: ...`) em
>   vez de virar objeto vazio. Campos a mais do que o formato prevê são erro
>   (`TextFile.TooManyFields`); campos faltando ficam a cargo da factory da entidade.
> - `games.txt` e `manga.txt` continuam usando a linha inteira como título, como o `FileService`.
> - O repositório não ordena: ordenação é da Application.
> - Decorator sem Scrutor: `AddTextFile<T, TParser>()` registra parser, repositório e um
>   `CachedReadRepository<T>` por factory. O cache usa `IMemoryCache` com o change token do
>   arquivo (`IFileProvider.Watch`), obtido antes da leitura.
> - Pacotes novos, todos `10.0.12`: `Caching.Memory`, `FileProviders.Physical`,
>   `Hosting.Abstractions`, `Logging.Abstractions`, `Options.ConfigurationExtensions`; nos testes,
>   `Configuration` e `DependencyInjection`.
> - BOM e `\r\n` são testados com bytes gravados num diretório temporário (fixture versionada com
>   `\r\n` dependeria do `core.autocrlf`). `Fixtures/` tem os 8 `.txt` com linha inválida, linha em
>   branco, capítulo `531.1` e BOM no `queue.txt`, e alimenta um teste de ponta a ponta
>   (handlers da Application + DI real da Infrastructure).
> - Conferido à parte: as 757 linhas reais dos 8 `.txt` passam pelos parsers sem erro.

### Fase 4 — Presentation ✅ concluída
1. Extrair `CoverGrid`, `CoverPreviewDialog`, `SearchBox`, `ListPager`, `TitleTable`.
2. Migrar página por página para os handlers (começar pela `MoviePage`, a mais simples com capa).
3. Mover `ProgressLabel` e cor por tipo para `Formatting/`.
4. Dashboard consumindo `DashboardSummaryDto`.

> **Notas da implementação:**
> - Saíram `Models/`, `Services/` (`FileService`, `IFileService`) e `Constants/` — o passo 5 da
>   fase 3 — e com eles a supressão do `CA1711` na classe `Queue`. Nenhuma página lê arquivo.
> - `CoverGrid<TItem>` é genérico porque Home (`InProgressItemDto`) e catálogo (`CatalogItemDto`)
>   têm DTOs diferentes: recebe `Load(search, page, ct)`, `Title`, `CoverUrl` e, opcionais,
>   `PlaceholderIcon`, `TitleIcon` (o ícone antes do título dos livros), `Badge` (o chip de tipo da
>   Home) e `Details` (progresso, episódio, autor). A busca e a paginação chamam o handler de novo;
>   uma requisição nova cancela a anterior, para uma resposta atrasada não sobrescrever a mais nova.
> - `TitleTable<TItem>` recebe a lista inteira (Game e Manga via `GetCatalogPage` com
>   `PageRequest.All`, Queue via `GetQueue`) e mantém busca e paginação do `MudTable` no cliente. O
>   número da linha é a posição na lista completa, fixado antes do filtro — antes era
>   `list.IndexOf(item)`, que com DTOs `record` daria o mesmo número para títulos repetidos.
> - O `GetCatalogPage` passou a buscar também pelo autor (só livros têm): a `BookPage` sempre
>   permitiu, e a fase 2 tinha deixado isso de fora. Teste novo na Application.
> - `SeriePage.razor` virou `SeriesPage.razor`; a rota `/serie` e o rótulo "Serie" do gráfico do
>   Dashboard ficaram iguais, para não mudar nada visível nesta fase.
> - Validação: o HTML pré-renderizado das 9 páginas tem os mesmos textos, imagens, classes e
>   atributos que a `main` (a única diferença é o `<h1>` não vir mais repetido no template de
>   streaming). Busca, paginação, alerta de lista vazia, lightbox, busca por autor, numeração da
>   fila filtrada e seleção de fatia no Dashboard foram conferidos no Chrome headless contra a
>   `main`, com o mesmo resultado.

### Fase 5 — Dados fora do `wwwroot` ✅ concluída
1. Mover `wwwroot/AppData/*.txt` para `AppData/`, na raiz do projeto, com
   `<Content Include="AppData\**" CopyToPublishDirectory="PreserveNewest" />`.
2. `Storage:DataPath` = `AppData` no `appsettings.json`.
3. Atualizar de novo a skill `update-covers` (ela lê `AppData/*.txt`).
4. As imagens continuam em `wwwroot/images/` — elas precisam ser públicas.

> **Notas da implementação:**
> - `git mv wwwroot/AppData AppData` (preserva o histórico dos 8 `.txt`); a pasta manteve o nome
>   `AppData`, sem o underline do `App_Data` do ASP.NET clássico. O `.csproj` trocou a `<Folder>`
>   e os 8 `<None>` por um único `<Content Include="AppData\**" ...>`; o publish põe os arquivos
>   em `/app/AppData/` e nada deles sobra no `wwwroot` nem no manifesto de assets estáticos.
> - Nenhum código mudou: o `DataPath` relativo já era resolvido pelo `ContentRootPath` (fase 3), que
>   é a pasta do projeto no `dotnet run` e `/app` no container.
> - `scripts/verify-docker.sh` ganhou duas checagens: `/AppData/movies.txt` e `/movies.txt`
>   precisam dar 404, e o log não pode ter `not found; the list is empty` nem `Skipping line` —
>   arquivo ausente é só *warning* e deixaria a página no ar com a lista vazia.
>   Conferido o caso negativo: com `Storage__DataPath=wwwroot/AppData` o diretório não existe e a
>   página dá 500 com `fail:` no log, o que o script já barrava.
> - Skill `update-covers`: tabela, caminho do `index.txt` e a checagem de órfão passaram para
>   `AppData/` na raiz do projeto (o `tmdb-covers.py` só mexe em `images/` e não
>   mudou). Checagem de órfão rodada com os caminhos novos: 0 órfãos.

### Fase 6 — Guarda-corpos
1. `WatchList.ArchitectureTests` com NetArchTest validando a tabela da seção 2.
2. Pipeline `dotnet build` + `dotnet test` (GitHub Actions ou similar).

---

## 7. Testes de arquitetura

```csharp
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_does_not_depend_on_other_layers() =>
        Types.InAssembly(typeof(Title).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("WatchList.Application", "WatchList.Infrastructure",
                                 "WatchList.Presentation", "Microsoft.AspNetCore")
            .GetResult().IsSuccessful.ShouldBeTrue();

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_presentation() => /* ... */;

    [Fact]
    public void Infrastructure_does_not_depend_on_presentation_or_aspnetcore() => /* ... */;
}
```

---

## 8. Convenções de código

- **Nomes:** inglês no código, PascalCase para tipos/membros, `_camelCase` para campos privados
  (ou primary constructors), sufixos por papel (`...Handler`, `...Query`, `...Dto`,
  `...LineParser`, `...Options`).
- **Namespaces** espelham pastas (`WatchList.Application.Catalog.GetCatalogPage`); file-scoped.
- **Visibilidade:** implementações da Infrastructure são `internal sealed`; só as extensões de DI
  são públicas. Classes são `sealed` por padrão.
- **Imutabilidade:** entidades e DTOs sem setters públicos; `record` para DTOs e value objects;
  coleções expostas como `IReadOnlyList<T>`.
- **Async:** todo método de I/O recebe `CancellationToken` e termina em `Async`.
- **Sem strings mágicas:** `MediaType` enum em vez de `"anime"`/`"series"`; nada de índice
  posicional como `data[5]`.
- **Erros esperados** (linha mal formatada) via `Result`; exceção só para o inesperado.
- **Logging** com `ILogger<T>` e mensagens estruturadas na Infrastructure.

---

## 9. O que fica de fora de propósito

- **MediatR / barramento de mensagens:** um handler injetado resolve; menos mágica, sem licença.
- **Repositório por entidade:** o genérico cobre leitura; crie um específico só quando surgir uma
  consulta que o genérico não expressa.
- **AutoMapper:** mapeamento manual em métodos `ToDto()` é explícito e fácil de depurar.
- **Banco de dados:** os `.txt` continuam sendo a fonte. A separação deixa a troca barata no
  futuro — seria um novo `IReadRepository<T>` na Infrastructure, sem tocar no resto.
- **Escrita/edição pela UI:** não existe hoje. Quando existir, é o momento de enriquecer as
  entidades com comportamento (ex.: `InProgressEntry.Advance()`) e criar comandos na Application.
