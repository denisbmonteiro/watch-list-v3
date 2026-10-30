# Plano: API REST na solution, com dados compartilhados

A Presentation só registra `AddApplication()` e `AddInfrastructure()` e chama os `IQueryHandler`. A
Infrastructure lê os `.txt` a partir de `Storage:DataPath`, um caminho relativo ao content root. Por
isso a API pode reaproveitar as camadas Application, Infrastructure e Domain sem mudar nada nelas.

O objetivo é ter a Presentation e a API funcionando em paralelo, lendo os mesmos arquivos, sem
afetar produção.

## 1. Onde ficam os `.txt`

**Decisão: uma pasta `data/` na raiz do repositório, fora de qualquer projeto.** Cada host aponta
para ela.

Colocar os arquivos na Infrastructure foi considerado e descartado:

- **Dado não é código.** A Infrastructure é o adaptador que sabe ler o formato `___`. O commit
  "chore: update watchlist" mexe só em dados; com essa opção, passaria a mexer num projeto de código.
- **Os arquivos se espalhariam.** Conteúdo copiado de uma biblioteca de classes vai para o `bin` de
  todo projeto que a referencia, inclusive o `WatchList.Infrastructure.Tests`. Os dados de produção
  apareceriam nos testes, que já têm `Fixtures/` próprias.
- **Não resolve o ambiente de desenvolvimento.** No `dotnet run`, o content root é a pasta do
  projeto, não o `bin`. A Infrastructure ainda precisaria de um caminho configurado por host.
- **Facilita o futuro.** Se um dia os dados forem para um volume no Dokploy ou para um banco, a pasta
  `data/` sai inteira e nenhum projeto muda.

**Como os dois hosts leem a mesma pasta, sem cópia no repositório:**

```xml
<!-- WatchList.Presentation.csproj e WatchList.Api.csproj -->
<Content Include="..\..\data\*.txt" LinkBase="AppData" CopyToPublishDirectory="PreserveNewest" />
```

| Ambiente | `Storage:DataPath` | Resultado |
|---|---|---|
| Produção (Docker) | `AppData` (em `appsettings.json`, como antes) | O publish gera `AppData/*.txt` ao lado da dll. A imagem fica idêntica à anterior. |
| Dev (`dotnet run`) | `../../data` em `appsettings.Development.json` | Os dois apps leem o arquivo original. O file watcher do `CachedReadRepository` invalida o cache nos dois quando um `.txt` é editado. |

## 2. Fase 1: mover os dados (PR 1, branch `refactor/shared-data`)

Esta é a parte que afeta produção, por isso vai sozinha num PR pequeno.

1. `git mv src/WatchList.Presentation/AppData/*.txt data/` (mantém o histórico).
2. No `WatchList.Presentation.csproj`, trocar `<Content Include="AppData\**" .../>` pelo item com
   `LinkBase` acima.
3. Colocar `Storage:DataPath: "../../data"` no `appsettings.Development.json` da Presentation.
4. Atualizar os caminhos que apontavam para `src/WatchList.Presentation/AppData`:
   - o `README.md`;
   - a skill `.claude/skills/update-covers/SKILL.md`, incluindo a checagem de órfãos com `awk`.

   O `tmdb-covers.py` só referencia `wwwroot`, então não muda. As capas continuam no `wwwroot` da
   Presentation.
5. Validar antes do merge, porque um push na `main` vai direto para a VPS:
   - `dotnet publish` e conferir se existe `publish/AppData/*.txt`;
   - `bash scripts/verify-docker.sh`. Ele confere se as páginas renderizam, se `/movies` mostra uma
     capa (sinal de que o `movies.txt` foi lido) e se `/AppData/movies.txt` continua inacessível.

## 3. Fase 2: projeto `WatchList.Api` (PR 2, branch `feat/api`)

**Estrutura:**

```
src/WatchList.Api/
  WatchList.Api.csproj            Sdk.Web → Application + Infrastructure + <Content> de data/
  Program.cs                      AddApplication().AddInfrastructure(config) + AddControllers + AddOpenApi
  appsettings.json                Storage:DataPath = "AppData"
  appsettings.Development.json    Storage:DataPath = "../../data"
  Properties/launchSettings.json  porta própria (ex.: 5286; a Presentation usa 5285)
  Controllers/
    CatalogControllerBase.cs      GET compartilhado, parametrizado por MediaType
    AnimeController.cs   MoviesController.cs   SeriesController.cs
    BooksController.cs   GamesController.cs    MangaController.cs
    InProgressController.cs       QueueController.cs
  Contracts/ListQuery.cs          search, pageIndex, pageSize com [Range]
tests/WatchList.Api.Tests/        WebApplicationFactory apontando DataPath para as Fixtures da Infrastructure
```

**Endpoints.** Seguem as mesmas rotas e os mesmos handlers da Presentation:

| Rota | Handler | Resposta |
|---|---|---|
| `GET /api/anime`, `/movies`, `/series`, `/books`, `/games`, `/manga` | `GetCatalogPageQuery(MediaType, search, page)` | `PagedResult<CatalogItemDto>` |
| `GET /api/in-progress` | `GetInProgressPageQuery` | `PagedResult<InProgressItemDto>` |
| `GET /api/queue` | `GetQueueQuery` | `IReadOnlyList<QueueItemDto>` (sem paginação, como a página `/queue`) |

**Decisões de implementação:**

- **Controllers só traduzem.** Cada um recebe HTTP e chama o handler, sem regra de negócio. Os seis
  de catálogo herdam de `CatalogControllerBase`, então cada um tem duas ou três linhas.
- **Paginação:**
  - `pageIndex` começa em 0, igual ao `PageRequest`;
  - o `pageSize` padrão é o primeiro de `ListPager.PageSizeOptions`, com um limite máximo (por
    exemplo, 100);
  - valores inválidos voltam como 400 `ProblemDetails` pela validação automática do
    `[ApiController]`, sem chegar ao `throw` do `PageRequest`.
- **Enums como texto.** Um `JsonStringEnumConverter` faz `MediaType` e `ProgressUnit` saírem como
  `"Anime"`, e não como `0`.
- **OpenAPI.** O `Microsoft.AspNetCore.OpenApi` nativo do .NET 10, exposto só em Development. A
  versão entra em `Directory.Packages.props`.
- **Testes.** Integração com `Microsoft.AspNetCore.Mvc.Testing`: um teste por rota, mais um de 400
  para paginação inválida. O projeto entra em `WatchList.slnx`.
- **Cache.** Cada processo tem o seu `IMemoryCache` e o seu watcher. Não há conflito, porque os dois
  só leem.

**Produção não muda nesta fase.** O `Dockerfile` publica só a Presentation, então o Dokploy continua
subindo o mesmo de antes. A API roda só localmente até a decisão de publicá-la.

## 4. Pontos em aberto

1. **URL das capas.** O `CoverUrlResolver` devolve `images/anime/x.jpg`, um caminho relativo ao site
   da Presentation, que é onde estão as imagens. Na API, esse caminho sozinho não serve para um
   cliente externo. Por agora fica assim, no mesmo padrão da Presentation. Quando um front consumir
   a API, entra uma opção `Covers:BaseUrl`, com padrão vazio para não afetar a Presentation.
2. **Dashboard.** Fora do escopo, que é só listagens. Se for preciso, `GET /api/dashboard` é mais um
   controller sobre o handler que já existe.
3. **CORS e autenticação.** Ficam para quando a API for exposta.
