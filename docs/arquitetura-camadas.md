# Plano de separação em camadas — WatchList

> Status: **em andamento** (fase 0 concluída) · Data: 23/09/2026 · Alvo: .NET 10 / C# 14

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
- `Extensions/EnumerableExtensions.cs` — `OrderByTitle`, `WhereIf`.

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
  tolerante a caixa (hoje é `string` comparada com `ToLower()` nas páginas).
- `Progress` — valor bruto + `ProgressUnit` (`Episode`, `Chapter`, `Page`, `SeasonEpisode`)
  derivado do `MediaType`. Guarda o texto original porque há capítulos como `531.1`.
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
│       ├── App_Data/                          # .txt saem do wwwroot (não ficam públicos)
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
│   ├── WatchList.Domain.Tests/
│   │   ├── WatchList.Domain.Tests.csproj
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
    <!-- testes -->
    <PackageVersion Include="xunit.v3" Version="..." />
    <PackageVersion Include="Shouldly" Version="..." />
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

**Branches:** cada fase é implementada numa branch local própria, criada a partir da `main`, com o
nome `refactor/fase-<n>` (`refactor/fase-0`, `refactor/fase-1`, …). Sem sufixo descritivo.

| Fase | Branch | Status |
|---|---|---|
| 0 — Preparação da solution | `refactor/fase-0` | ✅ concluída (24/09/2026) |
| 1 — Shared e Domain | `refactor/fase-1` | pendente |
| 2 — Application | `refactor/fase-2` | pendente |
| 3 — Infrastructure | `refactor/fase-3` | pendente |
| 4 — Presentation | `refactor/fase-4` | pendente |
| 5 — Dados fora do `wwwroot` | `refactor/fase-5` | pendente |
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

### Fase 1 — Shared e Domain
1. Criar `WatchList.Shared` (Guard, Result, extensões).
2. Criar `WatchList.Domain` com value objects e entidades; renomear `Serie` → `Series` e
   `Queue` → `QueueEntry`.
3. Escrever `WatchList.Domain.Tests` junto (value objects são o melhor retorno de teste aqui).

### Fase 2 — Application
1. Criar `IReadRepository<T>`, `ICoverUrlResolver`, `PagedResult<T>`, `PageRequest`.
2. Implementar os handlers: catálogo, em andamento, fila, dashboard.
3. Testes com repositório fake em memória.

### Fase 3 — Infrastructure
1. `TextFileReader` com `IFileProvider` + `StorageOptions`.
2. Um `ILineParser<T>` por arquivo; `TextFileRepository<T>` genérico.
3. `CoverUrlResolver` e `CachedReadRepository<T>`.
4. Testes de parser com fixtures reais (BOM, `\r\n`, campo faltando, capítulo `531.1`).
5. Remover `Services/FileService.cs`, `IFileService.cs` e `Constants/`.

### Fase 4 — Presentation
1. Extrair `CoverGrid`, `CoverPreviewDialog`, `SearchBox`, `ListPager`, `TitleTable`.
2. Migrar página por página para os handlers (começar pela `MoviePage`, a mais simples com capa).
3. Mover `ProgressLabel` e cor por tipo para `Formatting/`.
4. Dashboard consumindo `DashboardSummaryDto`.

### Fase 5 — Dados fora do `wwwroot` (recomendado)
1. Mover `wwwroot/AppData/*.txt` para `App_Data/` com
   `<Content Include="App_Data\**" CopyToPublishDirectory="PreserveNewest" />`.
2. `Storage:DataPath` = `App_Data` no `appsettings.json`.
3. Atualizar de novo a skill `update-covers` (ela lê `AppData/*.txt`).
4. As imagens continuam em `wwwroot/images/` — elas precisam ser públicas.

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
