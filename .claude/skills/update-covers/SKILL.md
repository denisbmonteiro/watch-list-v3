---
name: update-covers
description: Download and install cover art (posters) for the pending titles in the watch-list-v3 lists — movies.txt, series.txt, anime.txt. Use when asked to update, download or re-standardize capas/pôsteres/covers/posters/imagens for filmes, séries or animes in this project, or when lines without `___slug.jpg` show up in those files. Covers the TMDB API (v4 token, mandatory pacing against the CDN ceiling), picking clean untitled art, the slug rule, the 400px resize and what to report at the end. Instruções em português.
---

<!--
Cópia canônica dentro do projeto do prompt que vivia em ~/prompt-atualizar-capas.md.
Copiado em 2026-09-19 a partir da versão de 2026-09-18 17:18; renomeada de
`atualizar-capas` para `update-covers` (comando `/update-covers`) no mesmo dia.
Ao aprender algo novo numa leva (teto do CDN, desambiguação, colisão de slug), atualize
ESTE arquivo — é ele que é carregado quando a skill é invocada.
O script auxiliar agora mora junto: `scripts/tmdb-covers.py` (ver a seção sobre ele).
-->

Baixe as capas dos próximos N títulos sem imagem em um dos arquivos de lista do
projeto `watch-list-v3`. Eu digo qual lista (`filmes`, `séries` ou `animes`); se eu não
disser, pergunte. N é 10 se eu não disser outro número.

Pegue as N primeiras pendentes, **na ordem em que aparecem**, sem reordenar nem mexer
nas demais.

**ANTES DE BAIXAR QUALQUER COISA, cheque se a capa já não está no disco.** Linha pendente no
`.txt` **não** garante que falte imagem: já aconteceu de uma leva baixar e commitar as
imagens e não atualizar o `.txt`, deixando as capas órfãs e as linhas pendentes. Foi o commit
`f56b255` de 08/09/2026, que adicionou **20 capas de anime** (`Hagane no Renkinjutsushi` →
`Honzuki ... (2022)`) sem tocar no `anime.txt` — as duas levas de 14/09 foram resolvidas sem
baixar quase nada, só ligando o `.txt` ao que já existia.

Receita: monte o slug de cada pendente e teste `images/<dir>/<slug>.jpg`. Se existir, **não
rebaixe** — valide o que está lá (400px de largura, arte do título certo, sem tarja) e só
edite o `.txt`. Se a arte estiver errada ou tarjada, aí sim refaça só aquela. O jeito mais
rápido de ver isso de uma vez é a checagem de órfão da seção A QUARTA LISTA: **órfão em massa
logo nas próximas pendentes é exatamente esse sintoma.**

**Modo repadronização:** se eu disser "atualize mesmo se já tiver capa", pegue as N
primeiras linhas do arquivo — pendentes ou não — e refaça a arte de todas. Nesse modo
o `.txt` normalmente não muda (o slug já está lá), só as imagens.

---

# AS TRÊS LISTAS

Os caminhos abaixo são todos relativos a `src/WatchList.Presentation/wwwroot/`.

|                    | Filmes                          | Séries                                  | Animes                                        |
|--------------------|---------------------------------|-----------------------------------------|-----------------------------------------------|
| Arquivo            | `AppData/movies.txt`            | `AppData/series.txt`                    | `AppData/anime.txt`                           |
| Linha feita        | `Título___slug.jpg`             | `Título___SxxEyy___slug.jpg`            | `Título___slug.jpg`                           |
| Linha pendente     | `Título`                        | `Título___SxxEyy`                       | `Título`                                      |
| Busca              | `/3/search/movie`               | `/3/search/tv`                          | `/3/search/tv` (e `movie` p/ filme, OVA e especial) |
| Imagens            | `/3/movie/<id>/images`          | `/3/tv/<id>/images`                     | `/3/tv/<id>/images` **ou** `/3/tv/<id>/season/<n>/images` |
| Campos da busca    | `title`, `original_title`, `release_date` | `name`, `original_name`, `first_air_date` | iguais aos de série                   |
| Saída              | `images/movie/<slug>.jpg`       | `images/series/<slug>.jpg`              | `images/anime/<slug>.jpg`                     |
| Fim de linha       | LF                              | **CRLF**                                | **CRLF**                                      |
| Conta pendentes    | `awk -F'___' 'NF!=2'`           | `awk -F'___' 'NF!=3'`                   | `awk -F'___' 'NF!=2'`                         |

**O campo do meio das séries é o episódio em que eu parei — nunca mexa nele.** Ele é
útil como desempate: `S03E13` confirma que é a série com 13 episódios na 3ª temporada,
e foi o que separou `Marvel's Daredevil` (2015) de `Daredevil: Born Again` (2025).

**O `series.txt` e o `anime.txt` são CRLF.** Um `awk -F'___' '{print $3}'` devolve o nome do arquivo com
`\r` colado no fim, e aí a checagem "o arquivo existe em disco?" acusa falso negativo
nas 10 linhas. Faça `gsub(/\r/,"",$3)` antes de comparar. Preserve o CRLF ao editar —
não normalize o arquivo inteiro para LF de passagem. A forma segura de editar é ler o
arquivo em **binário**, dar `split(b"\r\n")`, mexer nas linhas e juntar de novo com
`b"\r\n"`; escrever em modo texto reescreve as quebras de linha sem avisar.

Ao editar o `series.txt` o git solta `warning: CRLF will be replaced by LF the next time
Git touches it`. **É esperado e não é erro seu**: o working tree segue CRLF, só o índice
normaliza. Não "conserte" isso convertendo o arquivo nem mexendo em `.gitattributes` —
se incomodar, é decisão do Denis, não da tarefa.

**Separador é `___` (três underscores).** Em 05/09/2026 a linha 22 do `series.txt` estava
como `Spartacus_S03E10`, com **um** underscore só — quebrava o parse e sumia com o
título. **Já foi corrigido a meu pedido nessa mesma data.** Continue checando: um
`awk -F'___' 'NF<2 || NF>3'` acha esse tipo de linha em um segundo, e vale rodar antes de
começar a leva. Se achar outra, **me avise e não corrija por conta própria** — só corrija
se eu pedir.

---

# A QUARTA LISTA: A HOMEPAGE (`index.txt`)

`src/WatchList.Presentation/wwwroot/AppData/index.txt` **não é uma lista de capas para esta tarefa** — é o
que aparece na homepage, o que eu estou assistindo/lendo agora. Nunca peça leva dele nem
edite esse arquivo. Ele importa aqui por um motivo só: **as imagens dele moram nos mesmos
diretórios das outras listas**, então ele muda a checagem de órfão.

Formato: **quatro** campos, `Título___Tipo___Episódio___slug.jpg`, CRLF, e a última linha
**sem newline no fim**. O `Tipo` (`Anime`, `Book`, `Manga`, …) diz em qual diretório está a
imagem — `Anime` → `images/anime/`, `Book` → `images/book/`, `Manga` → `images/manga/`.
Os diretórios `book/` e `manga/` existem **só** por causa dele.

**Consequência: comparar `images/anime/` só com o `anime.txt` acusa órfão falso.** Em
07/09/2026 isso deu 7 falsos positivos de uma vez — `one-piece.jpg`,
`bleach-sennen-kessen-hen-kashin-tan.jpg`, `honzuki-no-gekokujou-ryoushu-no-youjo.jpg`,
`mushoku-tensei-3.jpg`, `re-zero-kara-hajimeru-isekai-seikatsu-2026.jpg`,
`tensei-shitara-slime-datta-ken-2026.jpg` e `yomi-no-tsugai.jpg`. **Nenhum é órfão: são as
capas da homepage.** Não apague, não renomeie, não "conserte" nenhuma delas, e não as
inclua em leva nenhuma.

Alguns títulos da homepage têm linha *também* na lista da categoria, e outros não (a
homepage costuma ir na frente, com temporada que ainda nem entrou no `anime.txt`). Então a
ausência no `anime.txt` não diz nada. Alguns slugs da homepage são inclusive variantes que a
regra de SLUG não geraria sozinha (`honzuki-no-gekokujou-ryoushu-no-youjo` é um encurtamento
do título enorme) — **é ele quem manda para aquele arquivo**, não a regra.

A checagem de órfão correta une as duas fontes antes de comparar:

```bash
cd src/WatchList.Presentation/wwwroot
comm -23 <(ls images/anime | sort) \
  <(cat <(awk -F'___' 'NF==2 {gsub(/\r/,"",$2); print $2}' AppData/anime.txt) \
        <(awk -F'___' 'NF==4 && $2=="Anime" {gsub(/\r/,"",$4); print $4}' AppData/index.txt) \
    | sort)
```

Trocando `anime`/`Anime`/`NF!=2` pelos valores da lista certa, vale igual para filme e
série.

---

# A LISTA DE ANIMES

Formato **de filme** (`Título___slug.jpg`, dois campos, sem `SxxEyy`) com **fim de linha
de série (CRLF)** — vale tudo que está escrito acima sobre editar em binário e sobre o
warning do git. Conta pendente com `awk -F'___' 'NF!=2'`.

Os títulos são quase todos **romaji** e a busca é a de série; alguns são filme, OVA ou
especial e só aparecem em `/3/search/movie` (`Burn the Witch #0.8` é `movie/1179822`).
Cuidado com o live-action homônimo: `Detroit Metal City` existe como anime (`tv/43066`) e
como filme de 2008; `Boku dake ga Inai Machi` tem o anime de 2016 (`tv/65249`) e o drama
da Netflix de 2017; `Cowboy Bebop` tem o de 1998 (`tv/30991`) e o live-action de 2021.
O meu é sempre o anime.

## Muita linha minha é um cour, e o TMDB junta os cours numa temporada só

Não procure entrada avulsa: vá em `/3/tv/<id>/season/<n>/images` e ache a arte daquele
cour dentro do monte. Já mapeados (07/09/2026):

| Minhas linhas | Onde está no TMDB |
|---|---|
| `Bleach: Sennen Kessen Hen` + os 3 cours seguintes | S2 de `tv/30984` — 50 eps, os 4 cours juntos |
| `Code Geass` / `Code Geass R2` | S1 e S2 de `tv/31724` |
| `Chuunibyou demo Koi ga Shitai!` / `... Ren` | S1 e S2 de `tv/45501` |
| `Bokusatsu Tenshi Dokuro-chan` / `... 2` | S1 e S2 de `tv/42870` |
| `Dog Days` / ``Dog Days` `` / ``Dog Days`` `` | S1, S2 e S3 de `tv/44314` |
| `Darker Than Black: Kuro no Keiyakusha` / `... Ryuusei no Gemini` | S1 e S2 de `tv/31718` |
| As 8 linhas de `Dr. Stone` | `tv/86031`: S1, S2, **S0 ep3** (Ryuusui), S3 (2 cours), S4 (3 cours) |
| `Durarara!!` + `x2 Shou` / `Ten` / `Ketsu` | S1 e S2 de `tv/42410` — a S2 tem os 3 cours |
| As 4 linhas de `Full Metal Panic` | S1–S4 de `tv/39379` (Fumoffu é a S2, The Second Raid a S3) |
| `Fate/Zero` + `(2012)`; UBW `(2014)` + `(2015)` | S1 e S2 de `tv/45845` e de `tv/61415` |
| As 3 linhas de `Fumetsu no Anata e` | S1–S3 de `tv/97525` |
| `Golden Kamuy` + `(2018)` / `(2020)` / `OAD` / `(2022)` | `tv/76757`: S1, S2, S3, **S0** (specials), S4. A 6ª linha, `Saishuushou`, já estava feita antes |
| `Gin no Saji` / `... S2` | S1 e S2 de `tv/58237` ("Silver Spoon") |
| `Goblin Slayer` / `Goblin Slayer II` | S1 e S2 de `tv/82591` |
| `Hachimitsu to Clover` / `... II` | S1 e S2 de `tv/35415` (o anime de 2005 — `tv/48444` é o drama live-action de 2008) |
| `Hataraku Maou-sama!` / `!!` / `!! (2023)` | `tv/45997`: S1 (13 eps), e a S2 (24 eps) tem os 2 cours |
| `Hataraku Saibou` / `!!` | `tv/80671` S1 e S2 — `Black` é série à parte, `tv/101873` |
| As 4 linhas de `Ikkitousen` | S1–S4 de `tv/37584`, **sem inferência** (veja abaixo) |
| As 3 linhas de `Honzuki no Gekokujou` | `tv/91768` **S1**, que tem os 3 cours (36 eps = 14+12+10) |
| `Isekai Maou to Shoukan Shoujo no Dorei Majutsu` / `... Omega` | S1 e S2 de `tv/80563` — a S2 se chama literalmente `Ω` |
| `K` | **S1** de `tv/45799` (a S2 é `K: Return of Kings`, que não tem linha minha) |
| `K-ON!` | **S1** de `tv/42253`, 12 eps (a S2, `K-ON!!`, tem 24 e não tem linha minha) |
| `Kage no Jitsuryokusha ni Naritakute!` / `... 2nd Season` | S1 e S2 de `tv/119495` |
| As 4 linhas de `Kaguya-sama wa Kokurasetai` | `tv/83121`: S1 (`:`), S2 (`?`, `Love Is War?`, 2020), **S0 ep2** (a linha `?` com `(2021)`) e S3 (`Ultra Romantic`) |
| `Kami nomi zo Shiru Sekai` / `... II` / `... Megami Hen` | S1, S2 e S3 de `tv/56351` — os nomes das seasons batem (`II`, `Goddesses`) |
| `Kidou Senshi Gundam 00` / `... (2008)` | S1 e S2 de `tv/21732`, 25 eps cada |
| `Kidou Senshi Gundam: Tekketsu no Orphans` / `... (2016)` | `tv/64375` — **cour fundido**: 1 season só, com 50 eps (veja abaixo) |
| `Kino no Tabi: The Beautiful World` / `... - The Animated Series` | **séries separadas**: `tv/34166` (2003) e `tv/74097` (2017) |
| As 4 linhas de `Kono Subarashii Sekai ni Shukufuku o!` | S1–S3 de `tv/65844` (a S4 existe com 0 eps); `... ni Bakuen o!` é série à parte |
| `Kore wa Zombie Desuka?` / `... Of the Dead` | S1 e S2 de `tv/38420` — a S2 se chama literalmente `OF THE DEAD` |
| `Kusuriya no Hitorigoto` / `... (2025)` | `tv/220542` — **cour fundido**: 1 season só, com 48 eps (veja abaixo) |
| `Made in Abyss` / `... Retsujitsu no Ougonkyou` | S1 e S2 de `tv/72636` (a S2 se chama `The Golden City of the Scorching Sun`) |
| `Magi: The Labyrinth of Magic` / `... The Kingdom of Magic` | S1 e S2 de `tv/60654` — os nomes das seasons batem com os meus subtítulos |
| `Magi: Sindbad no Bouken` | `tv/66870` (TV, 2016, 13 eps). **Cuidado:** `tv/67340` é a OVA de 2014 (`type: Video`, 5 eps) da mesma obra |
| `Kuragehime` | `tv/37580`, o anime de 2010 — `tv/75984` é o drama live-action de 2018, mesmo nome e mesmo `original_name` |

**Quando o nome da season no TMDB bate com o meu subtítulo, acabou a investigação.** Uma
chamada a `/3/tv/<id>` lista as seasons com nome, ano e contagem de episódios, não custa
imagem nenhuma, e resolve sozinha os casos abaixo:

| Minha linha | Nome da season no TMDB |
|---|---|
| `... Omega` | `Ω` |
| `Kaguya-sama wa Kokurasetai? ...` | `Kaguya-sama: Love Is War?` |
| `Kaguya-sama wa Kokurasetai: Ultra Romantic` | `Kaguya-sama: Love Is War -Ultra Romantic-` |
| `Kage no Jitsuryokusha ni Naritakute! 2nd Season` | `Season 2` (com 12 eps, contra 20 da S1) |

**`Ikkitousen` é o caso mais fácil da lista inteira: os nomes das temporadas no TMDB batem
literalmente com os meus subtítulos** — S2 `Dragon Destiny`, S3 `Great Guardians`, S4
`Xtreme Xecutor`. Não precisa ordenar por enredo nem procurar pôster gêmeo; confira o nome da
season e pronto.

**`Honzuki no Gekokujou` foi reestruturado no TMDB (verificado em 14/09/2026) — não procure
`/season/3/`, não existe.** Os três cours (2019, 2020, 2022) foram fundidos na **S1, com 36
episódios**, e a **S2 passou a ser `Ryoushu no Youjo` (2026)** — que é a linha da *homepage*,
`honzuki-no-gekokujou-ryoushu-no-youjo.jpg`, e não uma das três do `anime.txt`. Como os cours
não têm entrada própria, a separação entre eles sai do enredo: o cour de 2022 é o do
Ferdinand com os mantos dourados e a Rozemyne de manto preto (arco da filha adotiva do
arquiduque), esse é seguro; **a divisão entre a linha sem ano e a de 2020 continua sendo
inferência não confirmada** — os cinco pôsteres japoneses do título **não** trazem marcador
`第X部`, então não há como fechar por letreiro.

**A OVA do Kaguya-sama é a linha `?` com `(2021)`, e mora na S0.** Verificado em
15/09/2026: o `S0E2` de `tv/83121` é "Kaguya Darkness", 2021-05-19, 24 min — a OVA que veio
com o volume 22 do mangá, catalogada no MAL sob o título com `?`. Bate com a interrogação
**e** com o ano da minha linha. A arte dela é a do elenco à paisana no sofá
(`x0NPV4bfJ8guuqKkKhhVRwB8SFL`, limpa); as outras do pool de especiais são do especial de
2025 ("Stairway to Adulthood"), então a atribuição sai por eliminação. **Corolário: ano
entre parênteses não é só cour — pode ser OVA**, e aí o lugar é a S0, igual ao `OAD` do
Golden Kamuy.

**`Tekketsu no Orphans`: os dois cours estão fundidos numa season de 50 eps e NENHUM pôster
traz marcador `第X期`.** A separação de 15/09/2026 saiu do **mech**, e é inferência minha:

| Minha linha | Arte | Sinal |
|---|---|---|
| `... Tekketsu no Orphans` | `i7JfdUax2YP4cPyVb6U0vJqCSSI` (limpa, 640x960) | **Barbatos**, com Mikazuki e Orga |
| `... Tekketsu no Orphans (2016)` | `nvcQ2GSbllZN5mdMgC2KfSs78bk` (letreiro ja, 2000x3000) | **Barbatos Lupus** — ombreira vermelha maciça, armadura lisa — e o Tekkadan já é uma multidão na poeira |

A arte do cour de 2016 **só existe com letreiro japonês**; a única em inglês
(`x5M9yxvBhrUP4rsKSl3d9VzK7O`, "IRON-BLOODED ORPHANS") é a do cour de 2015. Foi por isso
que a linha de 2016 ficou com a japonesa: não havia alternativa *para aquele cour*.

**`Kusuriya no Hitorigoto`: os dois cours estão fundidos numa season de 48 eps, e quem separa é
a AUSÊNCIA da tagline de estreia.** Achado de 15/09/2026, e é o inverso do truque do Bleach: em
vez de procurar o gêmeo *com* texto de estreia, procure o que **não tem**. Os pôsteres japoneses
de `/tv/220542/season/1/images` (sem filtro de idioma) trazem as taglines do cour de 2023 —
`薬屋の娘が宮中を揺るがす—` na arte do portal e na da Maomao de costas ao poente, e
`これ、毒です。` no close com os enfeites de cabelo. A arte noturna da Maomao com o Jinshi sob as
estrelas (`joSu7DQx6U7UqI22iFUlEbx1Q14` em ja, gêmeo limpo em `8HJzdLwQ0zxF7292vpETcg7uBox`) é a
**única sem tagline nenhuma** — logo, não é do lançamento, e foi para a linha de `(2025)`. Continua
sendo inferência, mas apoiada em letreiro e não em enredo.

**O gêmeo limpo pode estar no pool da SÉRIE e não no da temporada.** Em `Kore wa Zombie Desuka?`
(15/09/2026) os 4 candidatos de `/season/1/images` eram todos `en` — dois com tarja
"SEASON ONE"/"SEASON 01 COLLECTION" — e o gêmeo limpo da key art
(`rAafpc09GSyDST6mPsG0KAaNbqB`, `null`) só aparecia em `/3/tv/38420/images`. É a mesma regra do
`Dr. Stone`/`Darker Than Black` lá embaixo, mas vale também **quando o problema é achar o `null`,
não só quando é fugir da tarja.**

**`Gundam 00` separa por mech e é seguro.** As artes de temporada do `tv/21732` todas
trazem tarja "FIRST SEASON"/"SECOND SEASON" e foram descartadas; o conjunto da série
(`/3/tv/21732/images`) tem as duas artes limpas, e o mech decide: os quatro Gundams
iniciais (Exia, Dynames, Kyrios, Virtue) são a **S1**, e o **00 Raiser** — que só existe a
partir de 2008 — é a **S2**.

**Como achar a arte do cour certo:** procure o **pôster gêmeo com o texto de estreia**. Em
07/09/2026 o cour 4 do Bleach saiu assim — a mesma arte existia duas vezes, uma com
"相剋譚 / 2026-7 ON AIR" e outra limpa: a tarjada identificou o cour, a limpa foi
instalada. Sem esse gêmeo, ordene pelo enredo (Xeno só aparece na 2ª metade de
`New World`; o foguete é o arco final de `Science Future`; o "3" em pincelada é o
`x2 Ketsu`) e **me avise que a ordem é inferência sua** — foi o que aconteceu com os três
cours de Science Future e com o Durarara ×2.

**Ano entre parênteses na minha linha = cour, não remake.** `Dr. Stone: New World (2023)`
é o 2º cour de 2023, não outra série. Corolário: uma linha **sem** ano, de um título cujo
remake teria várias temporadas, costuma ser a obra **original** — foi assim que
`Fruits Basket` (linha única) virou o anime de 2001 e não o remake de 2019, que renderia
três linhas. Escolha assim, mas me avise.

---

# REGRA DE ACESSO — LEIA ANTES DE QUALQUER REQUISIÇÃO

**NÃO raspe o `www.themoviedb.org`.** Verificado em 2026-08-29: o
`https://www.themoviedb.org/robots.txt` lista 93 user-agents de IA com
`Disallow: /`, e entre eles estão `ClaudeBot`, `Claude-User`, `Claude-Web`,
`Claude-SearchBot` e `anthropic-ai`. Não há bloco `User-agent: *`, ou seja: o site é
aberto para crawler comum e fechado justamente para agente de IA. `Claude-User` é
exatamente a identidade de um agente agindo a pedido de você, então esse bloqueio se
aplica a esta tarefa.

Consequência prática: mandar User-Agent de browser para contornar isso é evasão do
robots.txt, não é otimização — e é a rota mais rápida para o IP levar bloqueio de
verdade. As versões antigas deste prompt mandavam fazer isso; **está revogado**.

**Use a API oficial**, que é gratuita, é o caminho que o TMDB oferece pra esse uso e
ainda é tecnicamente melhor (JSON, filtro de idioma real, sem parse de HTML):

- Chave grátis em <https://www.themoviedb.org/settings/api> (conta comum, aprovação
  imediata para uso pessoal).
- **A minha chave já existe: é o token v4 (Bearer) em `~/.config/tmdb/read_token`.**
  Leia o arquivo — **não espere env var**, eu não exporto `TMDB_API_KEY` nem
  `TMDB_READ_TOKEN` no shell. Se preferir env var:
  `export TMDB_READ_TOKEN=$(cat ~/.config/tmdb/read_token)`.
  **Nunca escreva a chave neste arquivo, em código commitado ou no scratchpad.**
- Só se `~/.config/tmdb/read_token` não existir é que você PARA e me pede a chave.
  Não caia no scraping.

Hosts liberados, sem restrição para agente:
- `api.themoviedb.org` — a API.
- `image.tmdb.org` — o CDN de imagem, cuja função documentada é justamente entregar
  os arquivos (`/robots.txt` dá 404; não é coberto pelo bloqueio do `www`).
- `developer.themoviedb.org` — a documentação (`Allow: /` para todos, e publica
  `llms.txt`). Se precisar de doc, `<url-da-página>.md` devolve markdown limpo.

## Autenticação com o token v4

Token v4 **não vai na query string** — vai no header, e o `api_key=` some da URL:

```
Authorization: Bearer <conteúdo de ~/.config/tmdb/read_token>
accept: application/json
```

Todas as URLs deste arquivo estão escritas na forma v3 (`?api_key=...`). Com o v4,
tire esse parâmetro e mande o header (o primeiro parâmetro que sobrar volta a usar `?`
em vez de `&`). O `image.tmdb.org` **não** quer header nenhum — só o `api.themoviedb.org`.

## O script auxiliar `scripts/tmdb-covers.py`

Mora **junto desta skill**, no repo: `.claude/skills/update-covers/scripts/tmdb-covers.py`.
Não é obrigatório — tudo aqui pode ser feito na mão com `curl` + `convert` — mas ele já
vem com as regras desta seção e da CADÊNCIA embutidas, então use-o em vez de reescrever
o laço de download a cada leva.

Ele é a versão adaptada do antigo `~/capas-tmdb.py` (que continua na home, intocado, e
**não** serve mais: é v3-only, movie-only e baixa com `urllib`). Já corrigidos aqui:
token v4 no header `Authorization`, `movie` **e** `tv` (com `--season`), download por
`curl` com `.part` + `os.replace`, delay de 1,5s com pausa de 3 min a cada 30 imagens,
contador de imagens que **persiste entre sessões** em `~/.cache/tmdb-covers/state.json`,
e `slugify` ASCII (`c.isalnum() and c.isascii()`) igual à seção SLUG.

```
./tmdb-covers.py probe                        # o CDN está liberado? (gasta 1 imagem)
./tmdb-covers.py validate                     # pipeline byte a byte, Fight Club (1 imagem)
./tmdb-covers.py budget                       # imagens gastas em 10 min / 1 h / 6 h
./tmdb-covers.py search tv "Spartacus"        # nenhuma imagem gasta
./tmdb-covers.py info tv 46296                # temporadas/episódios/país, para desempate
./tmdb-covers.py posters tv 46296 --season 0  # metadados dos pôsteres, sem baixar nada
./tmdb-covers.py triage tv 46296 spartacus --season 0 --n 6   # w342 + contact sheet
./tmdb-covers.py install /abc123.jpg spartacus series         # 400px direto no repo
./tmdb-covers.py slug "Don't Look Up"
```

A **escolha da arte continua manual** — o `triage` só monta a contact sheet; quem olha e
aplica a seção 3 é você. O `install` recusa sobrescrever capa que já existe (`--force`
só depois de eu confirmar), avisa quando a altura não sai 600, e escreve em
`images/movie|series|anime` conforme o terceiro argumento. O `posters` repete sozinho sem
`include_image_language` quando `en,null` não devolve nada, e filtra `aspect_ratio` fora
de 0,66–0,70. O caminho do repo sai da posição do próprio script — não tem home hardcoded.
Temporários vão para `~/.cache/tmdb-covers/work` (ou `CAPAS_WORK`), nunca para dentro do
projeto; `CAPAS_DELAY`, `CAPAS_PAUSE_EVERY` e `CAPAS_PAUSE` ajustam a cadência.

Conferido em 19/09/2026: `validate` saiu com **47790 bytes, `cmp` idêntico** ao
`images/movie/fight-club.jpg` do repo, e o download levou ~1s (ImageMagick 7.1.2-29 Q16).

**Nada de segredo no repo** — esta pasta `.claude/` é versionada. O token é lido de
`~/.config/tmdb/read_token` em tempo de execução, vai só no header `Authorization` e
**nunca** entra em URL, log ou mensagem de erro. Não cole token, `api_key` nem cabeçalho
preenchido neste arquivo nem no script; se precisar de exemplo, use
`$(cat ~/.config/tmdb/read_token)` como as URLs daqui já fazem. O script valida `kind`,
id, `--season`, slug, `file_path` e `--size` antes de montar URL ou caminho, não usa
`shell=True`, e recusa destino que escape de `images/<lista>/`.

## CADÊNCIA OBRIGATÓRIA — comece devagar, sempre

**Toda leva começa a 1,5s entre requisições de imagem, com pausa de 3 min a cada 30, sem
paralelizar. Não existe "sessão limpa o bastante para 0,5s".** Essa é a primeira regra a
seguir; o resto desta seção explica por quê.

Em 14/09/2026 o CDN travou (`http=000`) com **38 imagens acumuladas a ~0,5s**, em sessão que
tinha começado limpa — bem abaixo dos 85–129 que as medições anteriores sugeriam como teto.
Logo depois, **74 imagens a 1,5s com pausa de 3 min a cada 30 passaram sem um único `000` ou
`429`** (61 de triagem + 1 de validação + 10 de instalação + 2 avulsas), na mesma sessão.

**Ainda em 14/09/2026, na leva seguinte, o CDN travou com 2 imagens gastas na sessão — e
já a 1,5s.** Travou no primeiro download da triagem, e as 2 eram a sonda `w342` do
diagnóstico e o original do Fight Club da validação. O diagnóstico tinha respondido **200**
minutos antes, na mesma sessão: **sonda limpa não garante a janela.**

A lição que consolida todas as medições deste arquivo: **o que trava é rajada, não volume.**
O número de imagens é um mau preditor — 38 travaram, 74 não, e 2 travaram. O intervalo é o
que decide *dentro* da sua leva, mas ele não salva uma janela que já estava bloqueada antes
de a sessão começar (veja O CONTADOR NÃO ZERA ENTRE SESSÕES, logo abaixo).
Então não tente 0,5s "porque a leva é pequena" nem "porque a sessão está limpa": o custo de
começar a 1,5s é ~1 minuto a mais numa leva de 10, e o custo de errar é perder a leva inteira
e esperar de 6 a 40 min.

Corolário para o orçamento: **a 1,5s, triagem e instalação de uma leva de 10 cabem na mesma
janela** (foi o caso em 14/09). Continue tratando leva de 20 pelo plano B — triar tudo,
esperar, instalar.

**Rode o diagnóstico de 3 requisições antes de TODA leva, inclusive a primeira da
sessão**, e não no lugar do primeiro download. Numa das levas de 14/09 ele acusou o bloqueio
*antes* de eu gastar a triagem, o que transformou um prejuízo de leva inteira numa espera de
6 minutos. A versão antiga desta regra dispensava a primeira leva da sessão; **está
revogada**, porque foi exatamente uma primeira leva que travou depois.

### O CONTADOR NÃO ZERA ENTRE SESSÕES

Uma sessão que começa limpa pode já estar **dentro de uma janela bloqueada de antes** — o
CDN conta por IP e por tempo, não por sessão. Foi o caso da leva `Isekai Maou` →
`Kaguya-sama` de 14/09/2026: nada tinha sido baixado nessa sessão, e o primeiro download da
triagem já veio `000`.

Corolário: **"a sessão está limpa" não é evidência de nada.** Só o diagnóstico responde se dá
para começar — e responde só sobre o instante em que roda.

## Rate limit (doc oficial, `developer.themoviedb.org/docs/rate-limiting`)

- O limite antigo de 40 req / 10s foi **desativado em 16/12/2019**.
- Hoje existe um teto na casa de **~40 requisições por segundo**, descrito pelo
  próprio TMDB como proteção contra "needlessly high bulk scraping".
- **Respeite o `429`**: se vier, pare e espere. O limite pode mudar sem aviso.

Para uma leva de 50 capas isso é folgadíssimo — são ~100 chamadas de API. **Isso vale para
`api.themoviedb.org`, não para o CDN de imagem:** a API aguenta 0,2–0,4s tranquilamente e
nunca travou em medição nenhuma deste arquivo. O `image.tmdb.org` é que exige os 1,5s da
seção acima. Nunca paralelize nem um nem outro.

O travamento do `image.tmdb.org` descrito antigamente (~60 downloads seguidos, `http=000`) é
consistente com essa proteção anti-bulk: é o CDN te barrando por rajada, então o
intervalo importa mais que o volume total.

---

# FONTE PRINCIPAL: TMDB (pela API)

A vantagem sobre o IMDb é o **filtro por idioma** e o metadado de cada pôster
(proporção, tamanho, votos) antes de baixar. Na prática quase todo pôster sai em
alta resolução (800x1200 a 2000x3000).

## 1. Achar o título

```
https://api.themoviedb.org/3/search/movie?api_key=$TMDB_API_KEY&query=<url-encoded>   # filme
https://api.themoviedb.org/3/search/tv?api_key=$TMDB_API_KEY&query=<url-encoded>      # série
```
Resposta: `results[]` com `id` e — dependendo do endpoint — `title`/`name`,
`original_title`/`original_name`, `release_date`/`first_air_date`, `vote_count`. Sem
HTML, sem slug: o `id` é o que importa, e o problema antigo do link de anime vir sem
slug simplesmente deixa de existir.

Continua valendo a checagem de ano, mas agora com o dado certo: o `release_date` /
`first_air_date` da busca é a **estreia primária**, mais confiável que o ano que o site
exibe (que é regional). Ainda assim valide com margem de 1 ano, e para anime confira
também o `original_title`/`original_name` — foi assim que `Koe no Katachi` virou um
"A Silent Voice" homônimo qualquer numa leva antiga.

**Se a busca voltar vazia, encurte a query — não desista do TMDB antes disso.** O
`search/movie` engasga com título longo cheio de pontuação e romaji. Na leva de
31/08/2026, três dos 20 deram zero resultado com o título exato do `movies.txt` e
acharam de primeira com um fragmento curto:

```
One Piece "3D2Y": Overcoming Ace`s Death! ...            -> One Piece 3D2Y              (290271)
One Piece: Episode of Luffy - The Hand Island Adventure  -> One Piece Episode of Luffy  (172329)
One Piece (2000)                                         -> One Piece: The Movie        (19576)
```

Receita: tire o ano entre parênteses, tire tudo depois do `:` ou do ` - `, tire aspas e
pontuação, e busque só o nome distintivo.

### Desambiguação específica de série

Título curto de série colide muito mais que título de filme. Use, nesta ordem:

1. **O `SxxEyy` da própria linha.** Bateu a contagem de episódios da temporada, é ela.
2. **O resto da minha lista.** Gênero vizinho desempata: `Alone` (05/09/2026) deu
   *Home Alone* coreano, *Alone Together*, *Alone Australia* e o *Alone* do History
   (63726) — o do History é o certo, porque a minha lista tem `Naked and Afraid`.
3. **`vote_count`**, por último. O mais votado costuma ser o certo, mas não sempre:
   em `Daredevil` o topo era o *Born Again* de 2025, e o meu é o de 2015.

**Título de uma ou duas letras é o pior caso da busca, e o `original_name` resolve.** A
linha `K` (14/09/2026) devolveu, por votos: `K` (2012), `Music Universe K-909`, `K-ON!`,
`Dunaj, k vašim službám`, `K.C. Undercover` e `K Street` — ou seja, o `search/tv` casa a
letra solta com qualquer coisa que a contenha, inclusive em outro idioma. O desempate é o
`original_name` **igual à linha**: o de `tv/45799` é literalmente `K`. Não tente encurtar a
query nesses casos (já é mínima) nem confiar no `vote_count`; vá no `original_name`.

**Cuidado com reboot, revival e spin-off**, que têm nome quase igual e entrada separada
no TMDB — `Heroes` (1639) e `Heroes Reborn` (60858) são duas linhas distintas da minha
lista, e as quatro variações de `The Walking Dead` também.

## 2. Listar os pôsteres em inglês

```
https://api.themoviedb.org/3/movie/<id>/images?api_key=$TMDB_API_KEY&include_image_language=en,null
https://api.themoviedb.org/3/tv/<id>/images?api_key=$TMDB_API_KEY&include_image_language=en,null
```
Resposta: `posters[]` com `file_path`, `width`, `height`, `aspect_ratio`,
`vote_average`, `vote_count` e `iso_639_1`. Ordenada por votos.

`include_image_language=en,null` traz as artes em inglês **e** as sem idioma
marcado — muita arte teatral limpa está catalogada como `null`, então não filtre só
por `en` ou você perde justamente as melhores candidatas. Se não voltar nada, repita
sem o parâmetro para ver todos os idiomas.

Vantagem grande sobre o HTML: dá para triar por metadado antes de baixar um byte —
`aspect_ratio` fora de ~0.66–0.70 já elimina, e `width`/`height` mostram o formato
(veja os sinais de arte teatral na seção 3).

URL final da imagem: `https://image.tmdb.org/t/p/original/<file_path>`
(o `file_path` já vem com a barra na frente).

**Série tem MUITO mais pôster que filme** — `Game of Thrones` tem 196, `Daredevil` 99,
`Arrow` 86 — porque cada temporada ganha a sua arte. Triar 6 candidatos por título é
suficiente e é o que cabe no orçamento do CDN; o que interessa está no topo por votos.

### Quando a série volta com 0 ou 1 pôster

Duas saídas, nesta ordem, antes de partir para fallback:

**1. Repita sem `include_image_language`.** Já é a regra da seção 3c, mas em série ela
pega muito mais: `P.O.L.I.C.I.A` (05/09/2026) devolveu **zero** pôsteres em `en,null` e
**cinco** sem o filtro, todos `pt` — é série brasileira, então `pt` ali é a arte
original, não uma localização.

**2. Se o título for prequela, spin-off ou minissérie com subtítulo, procure-o como
temporada da série-mãe.** O TMDB frequentemente cataloga esses títulos as duas coisas ao
mesmo tempo, e a entrada avulsa costuma ser a pior das duas:

```
https://api.themoviedb.org/3/tv/<id>                        # lista as seasons e o poster de cada
https://api.themoviedb.org/3/tv/<id>/season/<n>/images       # todos os posteres daquela temporada
```

`Spartacus: Gods of the Arena` é o caso exemplar: a entrada avulsa (331850) tem **1
pôster**, 3 votos, e a única versão titulada dela é espanhola ("DIOS DE LA ARENA"). A
mesma obra está como **T0 do `Spartacus` principal (46296), com 18 pôsteres**, vários em
inglês — e os 6 episódios da T0 batem com o `S01E06` da linha. Sempre confira as duas.

O `/3/tv/<id>` também serve para **confirmar identidade** quando a busca é ambígua:
`origin_country`, `number_of_seasons` e `number_of_episodes` fecham a questão sem gastar
nenhuma imagem. Foi o que confirmou o `P.O.L.I.C.I.A` (BR, 4 temporadas × 13 episódios,
exatamente o `S04E13` da linha).

## 3. Escolher

### 3a. Filmes — o pôster teatral

**REGRA PRINCIPAL: o pôster teatral original, limpo.** A arte que o filme teve no
cinema, sem nenhuma tarja sobreposta. Nessa ordem de preferência:

1. One-sheet teatral original, sem tarja nenhuma.
2. O mesmo one-sheet com bloco de créditos no rodapé (aceitável, mas prefira a
   versão sem).
3. Só então artes alternativas — pôster de streaming/home video, arte de personagem
   solo, arte de relançamento.

Sinais de que é o teatral: elenco em composição, subtítulo/tagline do filme sob o
título, formato ~1012x1500 (o TMDB guarda o teatral nessa medida com frequência;
2000x3000 exato costuma ser arte de streaming). Nenhum é definitivo — olhe a imagem.

**Arte limpa ganha de proporção exata.** Os dois critérios brigam: o teatral limpo
costuma estar em 1012x1500 (0.675) enquanto a arte de streaming vem em 2000x3000
(2:3 cravado). Fique com a arte certa e aceite a altura sair 593 em vez de 600 —
o que não pode é cortar a imagem para forçar 2:3. Foi o caso do `12 Strong`.

### 3b. Séries — a key art da série, titulada

Série não tem "pôster de cinema", então o critério equivalente é: **a key art da
série, com o logo do título, limpa.** Nessa ordem:

1. Key art principal da série, com o logo, sem tarja.
2. A mesma arte sem título (`iso_639_1: null`) — aceitável, mas prefira a titulada
   quando as duas existirem, porque numa grade de capas o título ajuda a bater o olho.
3. Arte de uma temporada específica, se ela for a mais representativa da série.

**Prefira a arte da 1ª temporada / a arte "da série"** à arte da última temporada, que
costuma ser específica demais (personagem que só aparece no fim, visual de arco final).
Na leva de 05/09/2026 isso deu o one-sheet do Ned no Trono para `Game of Thrones` e a
cesta de basquete ao pôr do sol para `Fear the Walking Dead`, em vez das artes de S8.

**Case o idioma do letreiro com o título como está escrito na MINHA linha.** Quase toda
série estrangeira tem a mesma arte com o letreiro em vários idiomas, e a escolha certa é
a que combina com o que eu escrevi, não a que está mais bem votada. As quatro decisões de
05/09/2026:

| Linha no `.txt` | Idioma da linha | Arte escolhida |
|---|---|---|
| `O Mecanismo` | pt | `pt` "O MECANISMO" (a `en` dizia "THE MECHANISM") |
| `The Forest` | en | `en` "THE FOREST" (a original é `fr`, "LA FORÊT") |
| `The Snow Girl` | en | `en` "THE SNOW GIRL" (a original é `es`, "LA CHICA DE NIEVE") |
| `P.O.L.I.C.I.A` | pt | `pt`, única que existe — e é a original |

Isso **não** contradiz a regra de "não troque por arte localizada" da seção de anime:
lá o problema é trocar arte em inglês por arte em japonês/coreano quando o título é
inglês. Aqui é o contrário — o título da linha é que manda.

**Descarte sempre** (vale para filme e série): banner de data de estreia ("JANUARY 19",
"EM BREVE", "ONLY IN THEATERS", "NEW SEASON", "SERIES FINALE"), selo de formato
("EXPERIENCE IT IN IMAX", "4K Ultra HD", "Dolby"), tarja de edição ("EXTENDED EDITION",
"DIRECTOR'S CUT"), teaser, laurel de festival e **logo de plataforma** (Netflix, Prime,
Paramount+, HBO Max). Em série o logo de plataforma é o descarte mais frequente de
todos: o candidato de `Halo` mais bem posicionado trazia "Paramount+" no rodapé e a
tagline "RISE FROM THE FALL", e foi descartado por isso.

Descarte também **arte de personagem solo** (o TMDB cataloga a série inteira de
character posters) e **screenshot de episódio catalogado como pôster** — os dois passam
no filtro de proporção e só se pegam olhando a contact sheet. `Heroes` tinha um pôster
solo da Claire em 0.750 de proporção; `Dear Child` tinha um frame de cena entre os três
únicos pôsteres.

### 3c. Vale para os dois

**A arte limpa é obrigatória como primeira tentativa, e esgote as opções antes de
desistir dela.** Nessa ordem: (1) os outros pôsteres do mesmo título no TMDB,
inclusive os `null` de idioma; (2) `include_image_language` sem filtro, porque a
arte limpa às vezes está catalogada em outro idioma e mesmo assim é a mesma arte em
inglês; (3) o IMDb via GraphQL, que costuma ter a versão sem tarja de pôsteres que
o TMDB só tem tarjados.

Só se **nenhuma** das três der arte limpa, use a tarjada — e então **me avise no
final, com o nome do título e qual tarja sobrou**. Não troque silenciosamente arte
limpa por arte tarjada nem o contrário.

A lista vem **ordenada por votos**, e o mais votado costuma ser bom, mas **não é a
regra**. **Confira sempre antes de aceitar.** Dois casos reais: no *Furious 7* o mais
votado era o teaser do pneu, com duas variantes "EXTENDED EDITION"; no *12 Strong* o
topo era a arte de streaming (Hemsworth a cavalo sob o Chinook) enquanto o one-sheet
teatral — os quatro na crista da montanha — estava mais abaixo.

Para conferir 50 de uma vez sem baixar tudo em tamanho cheio: baixe os candidatos
em `/t/p/w342/`, junte com
`montage <arquivos> -resize 150x -tile 6x -geometry +4+4 <sheet>.jpg` e olhe a
imagem. É a única forma de pegar arte localizada, tarja e logo de plataforma — o
metadado da API filtra proporção, mas não enxerga nada disso.

Numa leva de 10 vale montar **uma linha por título, 6 candidatos**, e empilhar as
linhas numa folha só (`montage sheet-*.jpg -tile 1x`): 5 títulos por folha sai em
~1260x1550, que é legível de uma olhada. Ordene os candidatos pelo índice do JSON e
mantenha o nome `<slug>-<i>.jpg`, senão você perde a correspondência entre o que viu
na folha e o `file_path` que precisa baixar.

**Monte a folha a partir do manifesto, nunca com glob por prefixo.** Um
`f.startswith(slug + "-")` para `spartacus-` também casa com
`spartacus-gods-of-the-arena-`, e as duas séries saem intercaladas na mesma linha da
folha — aconteceu em 05/09/2026. O erro é silencioso e perigoso: a folha parece certa,
mas você escolhe a arte de uma série achando que é de outra. Gere a lista de arquivos a
a partir do `fila-manifest.json` (`[f"cand/{slug}-{i}.jpg" for i in range(len(man[slug]))]`)
e valide `getsize > 0` de cada um antes de montar. Um `-label '%f'` no `montage` escreve o
nome do arquivo embaixo de cada miniatura — é o jeito mais barato de não errar o índice na
hora de escolher, e vale a pena numa folha de zoom com poucos candidatos.

### Anime (e os filmes de One Piece em particular)

O TMDB cobre bem — na leva de 31/08/2026 os 17 filmes de One Piece saíram todos de lá,
sem precisar do AniList. Padrões que se repetiram:

- O **mais votado costuma ser o pior**: recorte antigo, baixa resolução ou com tarja
  preta no rodapé. A escolha boa quase sempre foi um **1500x2250 marcado `en`**, mais
  abaixo na lista, com o logo "ONE PIECE / <subtítulo>" limpo sobre a arte.
- Os `iso_639_1: null` costumam ser o **key visual sem título nenhum**. É arte limpa e
  legítima, mas para capa de watchlist o título ajuda — prefira a versão `en` titulada
  quando ela existir com a mesma arte.
- **Não troque por arte localizada.** Quando a única versão titulada é `ja`/`ko`/`zh`/
  `de`, fique com o `null` sem título. Foi o caso de `Episode of Merry` e `Episode of
  Nami`, cujas alternativas tituladas eram só japonesa, coreana, chinesa e alemã.
- Cuidado com **screenshot legendado catalogado como pôster**: passa no filtro de
  proporção e só se pega olhando a sheet (o `3D2Y` e o `Omatsuri` tinham um cada).

**Título romaji × arte com título traduzido** (regra fechada em 07/09/2026, depois de 60
capas do `anime.txt`). Quase toda linha minha está em romaji; a ordem de preferência é:

1. Arte com o logo em **alfabeto latino que seja o título mesmo** — `DURARARA!!`,
   `Chäos;HEAd`, `DENKI-GAI`, `BURN THE WITCH`, `DOG DAYS´`. É a melhor opção e ainda
   ajuda a diferenciar temporada (o `´` e o `″` do Dog Days estão no letreiro).
2. Arte **sem letreiro**, quando a única titulada traz o título **traduzido** para o
   inglês: `Chio's School Road`, `Delicious in Dungeon`, `I Can't Understand What My
   Husband Is Saying`. Aqui o `null` ganha do `en`. Mais sete em 14/09/2026, todos com
   gêmeo limpo da mesma arte: as duas linhas de `Isekai Maou ...` ("HOW NOT TO SUMMON A
   DEMON LORD"), `Isekai Ojisan` ("UNCLE from ANOTHER WORLD"), `Ishuzoku Reviewers`
   ("Interspecies Reviewers"), `Isshuukan Friends.` ("ONE WEEK FRIENDS") e as duas de
   `Kage no Jitsuryokusha ...` ("The Eminence in Shadow").
   **O caso 2 quase nunca custa resolução**: nesses sete o `null` limpo estava no mesmo
   endpoint que o `en` titulado, às vezes maior (o de `Isekai Ojisan` é 2000x3000). Procure
   o gêmeo antes de aceitar o titulado — é o mesmo padrão do pôster gêmeo com data de
   estreia e do gêmeo sem tarja de temporada.
3. Só então a traduzida, quando não existir versão limpa nenhuma — foi o caso de
   `Danshi Koukousei no Nichijou` ("DAILY LIVES OF HIGH SCHOOL BOYS") e de
   `Fumetsu no Anata e` ("TO YOUR ETERNITY"). Me avise quando cair aqui. Mais três em
   07/09/2026: `Goshuushou-sama Ninomiya-kun` ("GOOD LUCK! NINOMIYA-KUN") e as duas linhas
   de `Hachimitsu to Clover` ("honey and clover"). E mais seis em 14/09/2026: as duas linhas
   de `Hataraku Saibou` ("Cells at Work!" — o `original_name` é só `はたらく細胞`), as três
   de `Honzuki no Gekokujou` ("Ascendance of a Bookworm") e `Imouto sae Ireba Ii.`
   ("A SISTER'S ALL YOU NEED.").

**Cuidado com o caso 3 falso: confira o `original_name` antes de contar como traduzido.** A
mesma armadilha do `Gin no Saji` pegou duas vezes em 14/09/2026, com respostas opostas:

| Linha | `original_name` | Veredito |
|---|---|---|
| `Hagane no Renkinjutsushi: Brotherhood` | `鋼の錬金術師 FULLMETAL ALCHEMIST` | **caso 1** — o latim é o título oficial |
| `Hagane no Renkinjutsushi` (2003) | `鋼の錬金術師` | caso 3 — ali "FULLMETAL ALCHEMIST" é tradução |

**Letreiro híbrido — metade romaji, metade tradução — é CASO 2: prefira a arte sem
letreiro** (decisão minha de 15/09/2026, não pergunte mais). O
`Kaguya-sama wa Kokurasetai: Tensai-tachi no Ren`ai Zunousen` (14/09/2026) só tem arte com
"KAGUYA-SAMA / LOVE IS WAR": "KAGUYA-SAMA" é o romaji da minha linha, "LOVE IS WAR" traduz
`wa Kokurasetai`. Naquela leva foi tratado como caso 1 e a titulada foi instalada, porque a
única sem letreiro era arte de personagem solo de 744px. **A regra agora é a do caso 2:
procure o gêmeo limpo e instale ele sempre que existir com largura ≥ 400px.** Só caia na
titulada quando não houver gêmeo limpo — foi o que aconteceu com
`Kaguya-sama wa Kokurasetai: Ultra Romantic` em 15/09/2026, que **não tem um único pôster
sem letreiro** (só titulado em ja/zh/en/ko/ru/he/cu) e ficou com o `en`.

Em 15/09/2026 essa regra rendeu **15 das 20 capas da leva com arte limpa**, quase todas
gêmeas exatas da titulada e várias com resolução maior que ela — o mesmo padrão dos sete
casos de 14/09.

Ou seja, duas linhas quase idênticas da minha lista caem em casos diferentes. Uma consulta a
`/3/tv/<id>` resolve e não custa imagem nenhuma.

**Quando só existir arte com letreiro japonês e arte traduzida, e nenhuma limpa, me
pergunte.** Em 14/09/2026 o `Imouto sae Ireba Ii.` ficou exatamente nisso: nenhum dos 9
pôsteres é limpo, a versão `en` traz "A SISTER'S ALL YOU NEED." e a `ja`
(`tmXq2UNzCE68Ar9mTy3IWYyOdq3`) tem `妹さえいればいい。` como logo principal **mas carrega
"IMOUTO SAE IREBA II." em romaji na lateral**, batendo com a minha linha inteira. A
hierarquia acima manda escolher a traduzida, e foi o que ficou — mas a japonesa com romaji
é defensável e eu quero decidir esses. Não é o caso do `Grenadier`, onde o logo principal já
era latino.

**Latim no letreiro nem sempre é tradução — confira o `original_name` antes de contar como
caso 3.** `Gin no Saji` traz "Silver Spoon" na arte, mas o título japonês é
`銀の匙 Silver Spoon`: o latim faz parte do título oficial, então é **caso 1**, não caso 3.
O mesmo vale para `Grand Blue` → "GRAND BLUE DREAMING".

**E `iso_639_1: ja` não é motivo para descartar quando o letreiro é o título em latim.** Em
07/09/2026 as melhores artes de `GATE: Jieitai Kanochi nite, Kaku Tatakaeri` e de
`Grenadier: Hohoemi no Senshi` estavam catalogadas como `ja`, mas o logo é "GATE" e
"GRENADIER" em latim — e a do Grenadier ainda traz "Hohoemi no Senshi" em romaji no
rodapé, batendo com a minha linha inteira. A regra de descartar japonês vale para arte
**com o letreiro em japonês**, não para o campo de idioma da API.

Arte em russo, chinês ou japonês continua descartada **quando existe alternativa**:
`Dokuro-chan` e `Detroit Metal City` só tinham russo do outro lado, e em Dokuro-chan isso
custou ficar com um original de 400x578 (a saída sai em 400px de largura, sem upscale,
mas sem folga nenhuma).

**Descarte de anime que o TMDB adora catalogar como pôster de temporada:** tarja
"SEASON ONE/TWO/THREE/FOUR", "FIRST/SECOND SEASON", "Complete Collection" / "The Complete
Series", badge ①/② de volume, selo de classificação (MA15+) e capa de box de
distribuidora. Em temporada isso aparece em quase todo candidato do topo.

**A tarja de temporada quase sempre tem gêmeo limpo — procure antes de aceitar.** Em
14/09/2026 a capa instalada do `Honzuki ... (2022)` trazia "**Season Three**" no rodapé, e a
mesma arte existia sem a tarja em `zoLgYXZzKd833EPt1dUgMlr8gzJ` (1000x1500, `en`), mais
abaixo na lista. Vale o mesmo padrão do pôster gêmeo com data de estreia.

**Caixa de distribuidora estrangeira é descarte, e em anime antigo ela domina o topo.** As
quatro temporadas de `Ikkitousen` (14/09/2026) voltaram **zero** pôsteres em `en,null`, e no
fallback sem filtro vieram três caixas francesas da **KAZE**: "INTÉGRALE SAISON 1", "...
SAISON 2", "... SAISON 3", uma delas ainda com "Saison 3 Episodes 4 à 6" e logo de DVD. Os
sinais são o logo da distribuidora (KAZE, Dybex, Kazé, AnimeWorks), a palavra
`INTÉGRALE`/`SAISON`/`Vol.`, o selo "DVD"/"Blu-ray" e a moldura de capa de caixa.

**Marca d'água de site também é descarte, e só se pega olhando.** Um candidato do
`Ima, Soko ni Iru Boku` (14/09/2026) tinha "MEXAT.com" estampado no meio da arte. Passa em
qualquer filtro de metadado — proporção, tamanho e idioma estão todos certos.

**Pior que arte tarjada: arte de OUTRO título catalogada na entrada certa.** Em 15/09/2026 o
pôster `8qzkXwPrwdNeQ41ZPrWwwsulGoy`, listado em `tv/34166` (`Kino no Tabi`), era a capa de
**Noein - to your other self** — título, logo e elenco de outro anime. Proporção, tamanho e
`iso_639_1: null` todos plausíveis; só a contact sheet pega. É o argumento mais forte da
seção 3c: **nunca instale sem olhar a imagem**, nem quando o metadado está perfeito.

**O `/season/<n>/images` é onde mora a capa de volume de Blu-ray, e ela se disfarça de
pôster.** Em 14/09/2026 a S1 de `Isekai Maou ...` (`tv/80563`) devolveu, como 3 dos 5
candidatos, capas de volume: moldura branca, arte de personagem solo e o número do volume em
algarismo romano sob o logo ("A DEMON LORD **I**", "A DEMON LORD **2**") — mais um pôster de
travesseiro (tiras verticais de dakimakura) catalogado como pôster. Os sinais são a moldura
branca, o numeral avulso e a arte de um personagem só. A arte de verdade estava no conjunto
da série.

**Se a temporada só tiver capa tarjada, tente o conjunto da série** (`/3/tv/<id>/images`)
antes de aceitar: foi de lá que saiu a arte limpa do `Dr. Stone` S1 (Senku quebrando a
pedra) e do `Darker Than Black`, enquanto o `/season/<n>/images` só tinha "SEASON ONE".

## 4. Baixar

```
https://image.tmdb.org/t/p/original/<path>.jpg
convert <original> -resize 400x -quality 82 <destino>.jpg
```

**THROTTLING — planeje para isso:** depois de ~60 downloads seguidos de `original`,
o `image.tmdb.org` parou de responder por completo (curl `http=000`, timeout, não é
403 nem 429). O `media.themoviedb.org` não ajuda: ele só redireciona 301 para o
`image.tmdb.org` bloqueado. O bloqueio é temporário, mas custa a leva inteira se
acontecer no meio.

Isso casa com a proteção anti-bulk descrita na doc de rate limit — é rajada, não
volume. Então, numa leva de 50:

- **Intervalo de 0,2–0,5s entre downloads**, sem paralelizar. É o que evita o
  problema; não é opcional.
- Prefira `/t/p/w780/` a `/t/p/original/` quando o original passar de ~2000px —
  para uma saída de 400px o w780 é mais que suficiente e baixa uma fração dos
  bytes. O `width` da resposta da API já diz isso antes do download.
- **Orce a leva inteira antes de começar**, contando triagem + instalação:
  `N títulos × candidatos por título + N` tem que caber em **~115 imagens** (veja o teto
  medido abaixo). Numa leva de 20 isso só fecha com 4 candidatos por título, que é
  pouco para escolher arte — então o normal é **não caber**: trie em duas janelas
  separadas por ~15 min, ou trie tudo, espere o CDN, e só então instale.
- Se mesmo assim travar, **não abandone a leva de cara**: o bloqueio se solta sozinho.
  Faça um poll de 1 em 1 minuto no `w342` do Fight Club (a 2ª requisição do
  diagnóstico abaixo) por até 30 min, em background, e retome quando voltar 200. Só
  se não voltar nesse prazo é que você **para e me avisa** quantos títulos ficaram
  faltando. Nunca tente outro host nem outro UA para contornar.
- **A leva passa de 2 minutos de relógio** — só o delay obrigatório já dá ~40s em 115
  requisições. Se sua ferramenta de shell tiver timeout curto, rode em background e vá
  conferindo a saída; **não reduza o delay para caber no timeout**.

Reconferido em 31/08/2026, e o intervalo funciona *dentro de uma leva*: 95 downloads em
`w342` (triagem) mais 20 em `w780`/`original` (instalação), a 0,35s e sem paralelismo —
nenhum 429, nenhum `http=000`.

**Mas o orçamento é cumulativo entre levas, e `w342` conta.** Ainda em 31/08/2026, uma
segunda leva iniciada poucos minutos depois da primeira travou **no primeiro download**,
com ~116 imagens acumuladas na janela. Duas correções ao que estava escrito aqui:

- Não é só `original` que pesa — a **triagem em `w342` conta igual**. Uma leva de 40
  títulos gasta ~300 requisições de imagem só para montar as contact sheets.
- O contador **não zera** ao começar uma leva nova. Depois de uma leva grande, espere
  antes de começar a próxima; em leva de 40+, considere triar em duas metades.

**O teto está entre 115 e 129 imagens por janela, e o bloqueio dura de 16 a 27 min.** Terceira
medição de 31/08/2026, leva `Parasite`→`R.I.P.D.` (20 filmes), tudo a 0,35s e sem
paralelismo: triagem de 8 candidatos nos 10 primeiros (69 imagens) + 6 candidatos nos 10
últimos (58) + 2 de validação = **129 imagens**, e travou exatamente no **primeiro
download da instalação** — com a triagem inteira já gasta e nenhuma capa no disco.
Compare com os 115 que passaram limpos na leva anterior: o teto real da janela está
nessa faixa. Um poll de 1 em 1 minuto mostrou o CDN voltando a 200 na **13ª tentativa,
~16 minutos** depois; a instalação seguiu a 0,8s de delay sem novo travamento. Duas
lições:

- **A instalação é a parte cara de perder**, porque vem depois de toda a triagem já ter
  gasto o orçamento. Se a leva não couber em ~115, prefira travar *de propósito*: trie
  tudo, espere ~15 min, e só então instale.
- **Esperar funciona e é barato.** Não jogue a triagem fora por causa do `000` — o poll
  custa 1 requisição por minuto e devolve a leva inteira.

**Uma leva de 10 cabe numa janela; três levas seguidas não.** Medido em 05/09/2026, três
levas na mesma sessão, todas a 0,35–0,5s e sem paralelismo:

| Leva | Imagens | Início | Resultado |
|------|---------|--------|-----------|
| 1 (10 séries) | 68 (1 validação + 57 triagem `w342` + 10 instalação `w780`) | — | limpa |
| 2 (10 séries) | 66 (51 triagem + 4 extras + 1 sonda + 10 instalação) | 16,5 min após a leva 1 | limpa |
| 3 (1 série)   | **1** | 15,2 min após a leva 2 | **`000` no primeiro download** |

Ou seja: **N=10 numa janela limpa passa tranquilo** — 68 e 66 imagens não travaram nada.
Mas o acumulado **134 imagens em ~35 min** travou, e travou numa leva de *uma* capa só.

Três correções importantes ao que estava escrito acima:

- **O teto de 115–129 é o teto de uma leva contínua, não da janela.** Espaçar em ~16 min
  não zera o contador: as levas 1 e 2 somadas (134) estouraram mesmo com o intervalo.
- **Esperar 16 min entre levas não basta.** Aquele número é a duração do *bloqueio*, não
  o tamanho da janela do contador. Entre levas grandes, conte com bem mais do que isso.
- **Leva pequena não é segura por ser pequena.** O que decide é o acumulado recente, não
  o tamanho da leva atual — 1 imagem travou porque 134 vieram antes. Se você já rodou
  duas levas de 10 na sessão, **trate a terceira como se fosse grande**: rode o
  diagnóstico de 3 requisições antes, não no lugar do primeiro download.
- **O bloqueio pode durar bem mais que 16 min.** O de 05/09/2026 só soltou na **21ª
  tentativa do poll, ~26,5 min** — contra as 13 tentativas (~16 min) da medição de
  31/08. Não conclua "não vai liberar" antes das 30 tentativas; o poll de 1 em 1 minuto
  custa 1 requisição por minuto e nas duas vezes devolveu a leva inteira.
- **Delay maior + resfriamento resolvem — medido.** A leva seguinte àquele travamento
  foi configurada com **1,5s entre requisições, resfriamento de 25 min antes de encostar
  no CDN e pausa de 3 min a cada 30 imagens**, e passou **70 imagens (60 de triagem +
  10 de instalação) sem nenhum `429` nem `http=000`**, com o CDN respondendo 200 já na
  primeira sonda. É a configuração a usar sempre que a sessão já tiver gastado duas
  levas — e o resfriamento é a parte que mais importa, não o delay.

**Três levas de 20 numa sessão só, 333 imagens, nenhum bloqueio — medido em 07/09/2026**
(lista de animes, `Bleach: Sennen Kessen Hen - Soukoku Tan` → `Fumetsu no Anata e`):

| Leva | Imagens | Cadência | Intervalo desde a leva anterior | Resultado |
|------|---------|----------|--------------------------------|-----------|
| 1 (20 animes) | 118 (1 validação + 97 triagem `w342` + 20 instalação) | 0,5s na triagem / 0,8s na instalação, pausa de 3 min a cada 30 | — | limpa |
| 2 (20 animes) | 107 (82 triagem + 5 candidatos extras + 20 instalação) | 1,5s, pausa a cada 30 | 18 min de resfriamento | limpa |
| 3 (20 animes) | 108 (88 triagem + 20 instalação) | 1,5s, pausa a cada 30 | ~65 min + 5 min de resfriamento | limpa |

O que isso corrige do que está escrito acima: **o teto de ~115 por janela é do regime de
0,35–0,5s.** Com **1,5s + pausa de 3 min a cada 30 + resfriamento entre levas**, cada leva
de 20 fez triagem e instalação na mesma janela (107 e 108 imagens) sem `429` nem
`http=000`, e a sessão acumulou 333 imagens em ~5h. Compare com a medição de 05/09: 134
imagens em ~35 min a 0,35–0,5s **travaram**. Ou seja, o que trava é rajada, não volume —
e uma leva de 20 títulos com 4–6 candidatos cabe, desde que a cadência seja essa.

Na leva 1 a triagem e a instalação ficaram separadas por ~2h (esperei sem precisar) e
também passou limpa, então a receita antiga de "trie tudo, espere ~15 min, instale"
continua válida como plano B — só não é mais obrigatória para 20 títulos.

**O teto pode cair para ~86 e o bloqueio ser bem mais curto — medido em 07/09/2026**,
leva `Fumetsu no Anata e Season 2` → `Hachimitsu to Clover II` (20 animes), em sessão
limpa, sem leva anterior nenhuma:

| Fase | Imagens | Cadência | Resultado |
|------|---------|----------|-----------|
| Validação (Fight Club) | 1 | — | 200 |
| Triagem `w342` | 85 | 0,5s + pausa de 3 min a cada 30 | limpa |
| Instalação | — | 0,8s | **`000` no 1º download**, com 86 acumuladas |
| Poll de 1 em 1 min | 7 | 1/min | liberou na **7ª tentativa, ~7,7 min** |
| Instalação (retomada) | 20 | 1,5s | limpa |

Duas correções ao que está escrito acima:

- **O teto não é estável em 115–129.** Aqui travou com **86**, em sessão sem nenhuma leva
  anterior e já com pausa de 3 min a cada 30. Ou seja, o número varia por janela e não dá
  para orçar leva de 20 contando com 115. **Para leva de 20, trate ~85 como o teto de
  planejamento** — o que significa que triagem + instalação **não cabem** na mesma janela
  a 0,5s, mesmo com as pausas.
- **O bloqueio pode ser bem mais curto que 16 min.** Este soltou em ~7,7 min, contra os
  ~16 min de 31/08 e os ~26,5 min de 05/09. Reforça a regra: **poll de 1 em 1 minuto,
  nunca conclua nada antes das 30 tentativas** — o custo é 1 requisição por minuto e nas
  três medições devolveu a leva inteira.

O que **não** mudou: a triagem inteira passou limpa, o travamento foi exatamente no
primeiro download da instalação (a parte cara de perder), e retomar a 1,5s resolveu de
primeira. Então, para leva de 20, a receita segura continua sendo **triar tudo, esperar, e
só então instalar** — e instalar a 1,5s, não a 0,8s.

**O teto pode ser de apenas 38 imagens, e o bloqueio soltar em ~6 min — medido em
14/09/2026.** Sessão que começou limpa, leva de 10 animes:

| Fase | Imagens | Cadência | Resultado |
|------|---------|----------|-----------|
| Leva anterior (só triagem de desempate) | 38 | 0,5s | **`000` no diagnóstico seguinte**, com 38 acumuladas |
| Poll de 1 em 1 min | 6 | 1/min | liberou na **6ª tentativa, ~6,3 min** |
| Validação (Fight Club) + triagem + instalação | 74 | **1,5s + pausa de 3 min a cada 30** | limpa, nenhum `000`/`429` |

Essa é a medição que gerou a regra de CADÊNCIA OBRIGATÓRIA lá em cima, e ela **derruba de
vez a ideia de orçar leva por número de imagens**: 38 a 0,5s travaram, 74 a 1,5s não. Não
existe teto estável — as cinco medições deste arquivo já deram 38, 86, 115, 129 e 134. Pare
de orçar por volume e orce por cadência.

Repare também que o bloqueio de 14/09 foi **detectado pelo diagnóstico de 3 requisições,
antes da triagem**, e não pelo primeiro download da instalação. Custou 6 minutos de espera
em vez da leva inteira. É o argumento prático para rodar o diagnóstico sempre.

**O teto pode ser de 2 imagens, e o bloqueio passar de 39 min — medido em 14/09/2026**, leva
`Isekai Maou to Shoukan Shoujo no Dorei Majutsu` → `Kaguya-sama ... Ren`ai Zunousen` (10
animes), em sessão que **não tinha baixado nada** até ali:

| Fase | Imagens | Cadência | Resultado |
|------|---------|----------|-----------|
| Diagnóstico + validação (Fight Club) | 2 | — | API 200, CDN 200, pipeline bate os 47790 bytes |
| Triagem `w342` | 0 | 1,5s | **`000` no 1º download**, com 2 acumuladas na sessão |
| Poll de 1 em 1 min | 30 | 1/min | **`000` nas 30 tentativas (~39 min)** |
| Sonda avulsa logo após o poll | 1 | — | **200** |
| Triagem (retomada) + instalação | 72 | 1,5s + pausa de 3 min a cada 30 | limpa, nenhum `000`/`429` |

Três coisas novas, e nenhuma delas é sobre volume:

- **O contador não zera entre sessões** (regra completa na seção de CADÊNCIA). Aqui a sessão
  estava zerada e a janela, não.
- **A sonda do diagnóstico pode dar 200 e o bloqueio aparecer minutos depois.** Os dois
  primeiros downloads passaram; o terceiro, já a 1,5s, não. O diagnóstico vale pelo que
  economiza quando acusa, não como garantia quando libera.
- **O poll pode esgotar as 30 tentativas e o CDN liberar logo em seguida.** Este deu `000`
  por ~39 min — acima dos ~26,5 min que eram o máximo registrado — e respondeu 200 na
  primeira sonda depois do poll. **Sonde de novo antes de declarar leva perdida**; se ainda
  der `000`, rode outro poll de 30 antes de desistir.

**126 imagens a 1,5s numa sessão, leva de 20 inteira, limpa — e o bloqueio veio ANTES de
tudo, no diagnóstico. Medido em 15/09/2026**, leva `Kaguya-sama ?` → `KonoSuba`:

| Fase | Imagens | Cadência | Resultado |
|------|---------|----------|-----------|
| Diagnóstico, 1ª coisa da sessão | 1 | — | API 200, **CDN `000`** com nada baixado na sessão |
| Poll de 1 em 1 min | 10 | 1/min | liberou na **10ª tentativa, ~11,7 min** |
| Validação (Fight Club) | 1 | — | 200, bate os 47790 bytes |
| Triagem `w342` | 97 | 1,5s + pausa de 3 min a cada 30 | limpa |
| Candidatas extras `w342` | 9 | 1,5s | limpa |
| Resfriamento | — | 15 min | diagnóstico de novo: API 200, CDN 200 |
| Instalação (`w780`/`original`) | 20 | 1,5s | limpa |

**126 imagens na mesma sessão, nenhum `000` nem `429` depois do desbloqueio inicial.** Duas
coisas que isso confirma e uma que acrescenta:

- **Terceira vez seguida que uma sessão limpa começa dentro de janela bloqueada de antes.**
  O contador não zera entre sessões; "não baixei nada ainda" não é evidência de nada.
- **O diagnóstico se pagou de novo**, e pela terceira vez: acusou antes de gastar um único
  byte de triagem, transformando prejuízo de leva em espera de ~12 min.
- **A 1,5s com pausa a cada 30, leva de 20 cabe folgado** — 126 imagens contra as 38 que
  travaram a 0,5s. O resfriamento de 15 min entre triagem e instalação foi mantido (plano B)
  e o diagnóstico depois dele deu 200 de primeira.

**Na mesma sessão, a leva seguinte travou DUAS vezes mais — e o diagnóstico pegou as duas.**
Continuação de 15/09/2026, leva `KonoSuba 2` → `Magi: Sindbad no Bouken`:

| Momento | Acumulado na sessão | Resfriamento antes | CDN | Poll liberou em |
|---|---|---|---|---|
| Diagnóstico antes da triagem | 127 | 15 min | `000` | **3ª tentativa, ~3,7 min** |
| Triagem (101) + extras (9) | 240 | — | limpo | — |
| Diagnóstico antes da instalação | 240 | 18 min | `000` | **2ª tentativa, ~1 min** |
| Instalação (20) | 260 | — | limpo | — |

**260 imagens numa sessão, três bloqueios, zero leva perdida.** O que isso acrescenta:

- **Resfriamento de 15–18 min NÃO evita o bloqueio depois de ~130 imagens acumuladas** — as duas
  esperas foram respeitadas e o CDN travou nas duas. Não adianta esperar mais: adianta **rodar o
  diagnóstico e aceitar o poll**, que é barato.
- **Bloqueio tardio na sessão é curto.** O da abertura durou ~11,7 min; estes dois, ~3,7 e ~1 min.
  Padrão oposto ao de 14/09 (39 min). Não desista antes do poll.
- **O diagnóstico foi o que salvou a leva três vezes na mesma sessão.** Em nenhuma delas o `000`
  apareceu num download de verdade; em todas apareceu na sonda. Essa é a justificativa mais forte
  que esta seção tem para a regra de rodar o diagnóstico antes de **toda** fase — não só antes de
  toda leva, mas também **antes da instalação**, que é a parte cara de perder.

**Corolário operacional: não coloque o resfriamento dentro de um processo em background longo.**
Nesta sessão o script que fazia `sleep 900` e depois instalava foi **morto pelo sistema por falta
de memória** durante o sleep. Não custou nada (nada tinha sido baixado), mas se tivesse morrido no
meio da instalação teria deixado a leva pela metade. Faça a espera com um monitor leve ou em
pedaços, e rode **o download em primeiro plano** — 20 instalações a 1,5s levam ~40s, cabem
tranquilamente numa chamada só.

**Cuidado com arquivo de 0 byte:** `open(dest,"wb").write(get(...))` cria e trunca o
arquivo *antes* de o download terminar, então um travamento deixa um `.jpg` vazio — e o
`if not os.path.exists(dest)` do retry pula justamente o arquivo quebrado. Baixe para uma
variável primeiro e só então abra o arquivo, e no teste de "já baixei?" cheque
`os.path.getsize(dest) > 0` junto com o `exists`. O `scripts/tmdb-covers.py` tem esse
padrão no `fetch()`.

**BAIXE COM `curl`, NÃO COM `urllib` — o `urllib` desta máquina leva ~30s por imagem, e o
sintoma imita bloqueio de CDN.** Medido em 18/09/2026, mesma sessão e mesma URL do
`image.tmdb.org`: `urllib.request.urlopen(url, timeout=30).read()` gasta **~30,6s** (estoura o
`timeout` e mesmo assim devolve os bytes certos, íntegros), enquanto `curl` na mesma URL gasta
**~0,8s**. Não é o CDN: nos dois casos ele responde 200 na hora.

O sintoma é traiçoeiro porque parece exatamente throttle — a triagem anda a **1 imagem a cada
~30s**, o contador de arquivos rasteja, e a conclusão natural é "estou levando rajada". Numa leva
de 20 a diferença é de ~4 min para ~50 min de triagem, e o risco real é gastar poll e
resfriamento atrás de um bloqueio que não existe. **Antes de concluir qualquer coisa sobre
lentidão, rode um `curl` cronometrado** — uma requisição só separa os dois casos:

```bash
curl -s -o /dev/null -w "code=%{http_code} conn=%{time_connect} total=%{time_total}\n" \
  --max-time 40 https://image.tmdb.org/t/p/w342/jSziioSwPVrOy9Yow3XhWIBDjq1.jpg
```

`code=200` com `total` abaixo de 1s = o CDN está liberado e o problema é a sua biblioteca.

O padrão a usar no script de download, que já junta isto com a regra do arquivo de 0 byte:

```python
tmp = dest + ".part"
r = subprocess.run(["curl", "-s", "-o", tmp, "-w", "%{http_code}", "--max-time", "30", url],
                   capture_output=True, text=True)
if r.stdout.strip() == "200" and os.path.getsize(tmp) > 0:
    os.replace(tmp, dest)          # só vira o destino depois de completo
else:
    os.remove(tmp)                 # registra a falha e SEGUE — não aborte na primeira
time.sleep(1.5)                    # a cadência continua sendo a da CADÊNCIA OBRIGATÓRIA
```

O `.part` + `os.replace` resolve o 0 byte sem precisar segurar a imagem em memória, e o `curl`
traz de graça o `%{http_code}`, que é o que distingue `000` (bloqueio) de 200 lento.

**Cuidado com um segundo disfarce do mesmo problema:** processo em background parece
"estrangulado pelo ambiente" quando na verdade é o `urllib` dentro dele. Em 18/09 cheguei a
atribuir a lentidão ao background — era o `urllib`, e o mesmo script ficou rápido em background
depois da troca para `curl`. Isso **não** revoga a regra de rodar a instalação em primeiro plano
(aquela é sobre kill por memória, no parágrafo acima), só o diagnóstico errado.

**Como confirmar que é o CDN e não a rede** (leva 3 requisições):

```
curl -s -o /dev/null -w "%{http_code}\n" --max-time 20 \
  -H "Authorization: Bearer $(cat ~/.config/tmdb/read_token)" \
  https://api.themoviedb.org/3/movie/550          # 200 = API ok
curl -s -o /dev/null -w "%{http_code}\n" --max-time 20 \
  https://image.tmdb.org/t/p/w342/jSziioSwPVrOy9Yow3XhWIBDjq1.jpg   # 000 = CDN travado
curl -s -o /dev/null -w "%{http_code}\n" --max-time 20 https://graphql.anilist.co
```
API 200 + CDN 000 + terceiro host respondendo = é o bloqueio anti-rajada, não a sua rede.
(O `graphql.anilist.co` hoje devolve **404** num GET simples — para efeito de sonda isso
conta como "respondendo"; o que importa é não dar timeout.)
Aí entre no poll de 1 em 1 minuto (regra acima) e retome quando voltar 200. **Se as 30
tentativas esgotarem, sonde uma vez mais antes de me avisar como leva perdida** — em
14/09/2026 o CDN deu `000` nas 30 e voltou 200 na sonda seguinte; se essa também der `000`,
rode um segundo poll de 30 e só então desista. Em Python o sintoma não é `403` nem
`429` — é `URLError: [Errno 101] Network is unreachable`, que parece problema de rede
local. Rode o diagnóstico antes de concluir qualquer coisa. **E se o sintoma for lentidão em vez
de erro, não é bloqueio nenhum: é o `urllib`** — veja BAIXE COM `curl` na seção 4.

```bash
for i in $(seq 1 30); do
  c=$(curl -s -o /dev/null -w "%{http_code}" --max-time 20 \
      https://image.tmdb.org/t/p/w342/jSziioSwPVrOy9Yow3XhWIBDjq1.jpg)
  echo "$(date +%H:%M:%S) tentativa $i -> $c"
  [ "$c" = "200" ] && { echo "CDN LIBEROU"; exit 0; }
  sleep 60
done
# as 30 podem esgotar e o CDN liberar logo depois — sonde mais uma vez antes de desistir
sleep 60
c=$(curl -s -o /dev/null -w "%{http_code}" --max-time 20 \
    https://image.tmdb.org/t/p/w342/jSziioSwPVrOy9Yow3XhWIBDjq1.jpg)
[ "$c" = "200" ] && { echo "CDN LIBEROU na sonda extra"; exit 0; }
echo "AINDA BLOQUEADO apos 30min + sonda extra"; exit 1
```

---

# FALLBACKS

## Live-action (filme ou série) → IMDb

1. `https://v3.sg.media-imdb.com/suggestion/x/<query>.json?includeVideos=0`
   (query em minúsculas, url-encoded) → `d[].i.imageUrl` é a imagem principal.
   O campo `q` de cada resultado diz `feature` / `TV series`, então dá para filtrar o
   tipo certo antes de escolher o `tt`.
2. Remova o sufixo `._V1_*.jpg` da URL e baixe `<base>._V1_QL75_UX400_.jpg`.
   Esse resizer da Amazon já entrega 400px de largura com o aspecto preservado —
   não redimensione localmente.

**Atenção com a regra do pôster limpo:** a `primaryImage` do IMDb costuma ser o
one-sheet teatral *com a tarja de estreia* — foi o caso do `12 Strong`, que veio com
"JANUARY 19 / EXPERIENCE IT IN IMAX" e bloco de créditos. Nesses casos vale voltar
ao TMDB, que quase sempre tem a mesma arte sem tarja, ou listar os pôsteres via
GraphQL e procurar a versão limpa.

Se a imagem principal não for um pôster de verdade (proporção fora de ~0.67–0.71),
liste todos os pôsteres do título e escolha outro:
```
https://caching.graphql.imdb.com/?operationName=x&query=<urlencode(
  'query x {title(id:"tt...."){titleText{text} primaryImage{url width height}
   images(first:60,filter:{types:["poster"]}){total edges{node{url width height}}}}}'
)>
```
Headers: `content-type: application/json`, `x-imdb-client-name: imdb-web-next`,
User-Agent de browser. A query PRECISA ser operação nomeada (`query x {...}`) casando
com `operationName`, e a URL precisa usar `%20` — `curl -G --data-urlencode` manda `+`
e o servidor rejeita. Não tente `www.imdb.com` (202 vazio) nem `api.graphql.imdb.com`
(403). O mesmo `title(id:)` serve para série — o IMDb não separa os tipos aqui.

Para escolher entre os candidatos, baixe em `._V1_QL75_UX220_.jpg` e monte a
contact sheet — arte em alemão/coreano/japonês costuma aparecer no topo da lista.

## Anime → AniList — **FORA DO AR desde (pelo menos) 07/09/2026**

Verificado em 07/09/2026: qualquer POST devolve `403` com
`{"message":"The AniList API has been temporarily disabled due to severe stability
issues."}`. **Não é o 403 por falta de User-Agent** descrito abaixo — acontece igual com
UA de browser. Um GET simples no host devolve `404`, então ele não serve nem como sonda
do diagnóstico de rede; use outro host ali.

Enquanto durar, **anime tem só o TMDB** (o IMDb continua valendo para live-action). Onde
isso dói é justamente em separar cour de temporada, porque o AniList tem uma entrada por
cour com data e o TMDB não — veja a seção A LISTA DE ANIMES para o que fazer no lugar.
Tente de novo a cada leva: se voltar, o que está escrito abaixo continua válido.

```
POST https://graphql.anilist.co   (Content-Type: application/json)
{"query":"query($s:String){Page(perPage:5){media(search:$s,type:ANIME){id title{romaji english} format seasonYear coverImage{extraLarge}}}}",
 "variables":{"s":"<título>"}}
```
**Precisa de User-Agent de browser** — sem ele devolve 403.
`coverImage.extraLarge` vem em 460px de largura. Reduza com
`convert <original> -resize 400x -quality 82 <destino>.jpg`.
O `format` distingue `TV` de `MOVIE`, então serve para as duas listas.

**Atenção:** a capa do AniList às vezes é um *crop* — no filme Code Geass I ela
cortava o logo do título. Se a arte parecer apertada, confira o TMDB/IMDb.

---

# FONTES QUE NÃO VALEM O ESFORÇO

- **MyAnimeList**: funciona com UA de browser (`/anime/<id>/_/pics`, sufixo `l`
  antes do `.jpg` é a versão grande), mas entrega ~320x450. Inútil para 400px.
- **API Jikan**: 504 nas tentativas de 28 e 29/08/2026.
- **AniDB**: o site é 403 para requisição automatizada. O **CDN é aberto**
  (`https://cdn.anidb.net/images/main/<arquivo>.jpg` responde 200 sem headers
  especiais), mas o nome do arquivo só sai da `httpapi`
  (`http://api.anidb.net:9001/httpapi?request=anime&client=<nome>&clientver=1&protover=1&aid=<aid>`),
  que exige um **client registrado** — com nome inventado devolve
  `<error code="302">client version missing or invalid</error>`. Denis pode
  registrar um client grátis no perfil do AniDB (Add Project → HTTP client);
  se ele passar o nome, dá para usar, respeitando o limite de 1 req / ~2s.

---

# SLUG

Vale igual para filme e série, e sai **só do título** — o `SxxEyy` da série **não**
entra no slug.

Minúsculas; apóstrofo é REMOVIDO; todo o resto que não for `[a-z0-9]` vira hífen;
hifens colapsados e aparados nas pontas.
```
Don't Look Up        → dont-look-up          Doctor Strange (2016) → doctor-strange-2016
Dune: Part One       → dune-part-one         Code 8                → code-8
Goblin Slayer: Goblin`s Crown → goblin-slayer-goblins-crown   (crase tratada como apóstrofo)
Heroes Reborn        → heroes-reborn         P.O.L.I.C.I.A         → p-o-l-i-c-i-a
```

**Colisão de slug — a crase que some.** `Dog Days`, ``Dog Days` `` e ``Dog Days`` `` são
três linhas distintas do `anime.txt`, e a regra "apóstrofo é REMOVIDO" colapsa as três em
`dog-days`. Decisão minha em 07/09/2026: **sufixo numérico por ordem de temporada** —
`dog-days.jpg`, `dog-days-2.jpg`, `dog-days-3.jpg`, no mesmo padrão de
`bokusatsu-tenshi-dokuro-chan-2`. **As linhas do `.txt` não mudam**, só o nome do arquivo.
Se aparecer uma colisão nova que essa regra não cubra, **pergunte antes de inventar**.

**A mesma regra cobre colisão por `!` de temporada** — aplicada em 14/09/2026 sem perguntar,
por ser o mesmo caso da crase:

| Linhas | Arquivos |
|---|---|
| `Hataraku Maou-sama!` / `Hataraku Maou-sama!!` | `hataraku-maou-sama.jpg` / `hataraku-maou-sama-2.jpg` |
| `Hataraku Saibou` / `Hataraku Saibou!!` | `hataraku-saibou.jpg` / `hataraku-saibou-2.jpg` |

**Colisão de pontuação `:` contra `?` (Kaguya-sama) — RESOLVIDA, sufixo numérico.** As
linhas `Kaguya-sama wa Kokurasetai: Tensai-tachi no Ren`ai Zunousen` (dois-pontos) e
`Kaguya-sama wa Kokurasetai? Tensai-tachi no Ren`ai Zunousen` (interrogação) colapsam
no mesmo slug, porque `:` e `?` viram hífen e o hífen duplo colapsa. Decisão minha de
15/09/2026: **é o mesmo caso da crase e do `!`, sufixo numérico por ordem de temporada.**

| Linha | Arquivo |
|---|---|
| `... Kokurasetai: Tensai-tachi ...` (S1) | `kaguya-sama-wa-kokurasetai-tensai-tachi-no-renai-zunousen.jpg` |
| `... Kokurasetai? Tensai-tachi ...` (S2) | `kaguya-sama-wa-kokurasetai-tensai-tachi-no-renai-zunousen-2.jpg` |
| `... Kokurasetai? Tensai-tachi ... (2021)` (OVA) | `kaguya-sama-wa-kokurasetai-tensai-tachi-no-renai-zunousen-2021.jpg` |

A terceira, com `(2021)`, usa o sufixo de ano e não colide com nada.

Note que o sufixo numérico **convive com o sufixo de ano** sem ambiguidade:
`hataraku-maou-sama-2.jpg` é o cour de 2022 e `hataraku-maou-sama-2023.jpg` é o de 2023, que
vem da linha `Hataraku Maou-sama!! (2023)`.

No `Hataraku Saibou` o próprio letreiro confirma a ordem — a arte da S1 diz "Cells at Work!"
e a da S2 "Cells at Work!!". **Quando a pontuação estiver no letreiro, use isso como prova em
vez de inferir**, igual ao `´`/`″` do Dog Days.

# SAÍDA

- Salve cada imagem em `src/WatchList.Presentation/wwwroot/images/movie/<slug>.jpg` (filme) ou
  `src/WatchList.Presentation/wwwroot/images/series/<slug>.jpg` (série).
- Troque a linha correspondente do `.txt` de `Título` para `Título___slug.jpg`
  (filme) ou de `Título___SxxEyy` para `Título___SxxEyy___slug.jpg` (série),
  preservando o título e o episódio exatamente como estão escritos (inclusive typos
  — só me avise). No modo repadronização a linha já está completa: não a reescreva.
**NÃO conte pendentes com `grep -vc`.** No shell desta máquina o `grep` é uma função
que roteia para o **ugrep**, e o `-v -c` combinado **devolve 0** quando a última linha do
arquivo não termina em newline — que é exatamente o caso do `movies.txt`. Reproduzido em
06/09/2026:

```
printf 'a.jpg\nb\nc.jpg\n' > com_nl.txt   # ugrep -vc = 1   GNU -vc = 1
printf 'a.jpg\nb\nc.jpg'   > sem_nl.txt   # ugrep -vc = 0   GNU -vc = 1  <-- erra
```

O sintoma é péssimo: ele diz "nenhum pendente" e a leva inteira parece já estar pronta.
Conte com **`awk -F'___' 'NF!=2' movies.txt | wc -l`** (ou `NF!=3` para o `series.txt`),
que é o mesmo comando que já acha linha malformada. `grep -q` e `grep -c` sem o `-v` estão
corretos — a checagem de órfão pode continuar como está.

- **Confira no final**, com o `\r` removido antes de comparar:
  - todo arquivo referenciado no `.txt` existe em disco;
  - toda imagem nova tem 400px de largura;
  - não sobrou órfão no diretório de imagens — **unindo o `.txt` da lista com o
    `index.txt` da homepage** antes de comparar, senão as capas da homepage aparecem
    como órfãs (veja A QUARTA LISTA);
  - o `git status` mostra só as imagens (e o `.txt`, quando houve linha nova) — nada
    mais.

**Duas capas antigas do `images/anime/` estão fora dos 400px e NÃO são erro seu** (achadas em
14/09/2026 por um `identify` no diretório inteiro): `air-gear-kuro-no-hane-to-nemuri-no-mori-
break-on-the-sky.jpg` com 346px e `bakemonogatari.jpg` com 354px. São de levas antigas.
**Não refaça por conta própria** — se quiser corrigir, eu peço. Se aparecer uma terceira,
me avise sem mexer.
- **NÃO faça commit**: deixe tudo no working tree. E não deixe arquivo temporário
  dentro do projeto — use o scratchpad da sessão (já aconteceu de um `tmdb.html`
  vazar para a raiz por rodar `curl -o` sem `cd`).

# VALIDAÇÃO DO PIPELINE

Antes de baixar a leva, reproduza uma capa que já existe pelo caminho do TMDB:
```
https://image.tmdb.org/t/p/original/jSziioSwPVrOy9Yow3XhWIBDjq1.jpg   (Fight Club, TMDB 550)
convert <original> -resize 400x -quality 82 fight-club.jpg
```
tem que sair idêntico a `src/WatchList.Presentation/wwwroot/images/movie/fight-club.jpg` — 400x600,
**47790 bytes** com ImageMagick 7.1.2 Q16.

Esse número foi **reconferido em 2026-08-29 e de novo em 2026-09-05** (`cmp` byte a
byte, ImageMagick 7.1.2-29 Q16). Vale como validação para as duas listas — é o
mesmo pipeline de download e resize. Se der diferença de poucos bytes, suspeite da
versão do ImageMagick antes de suspeitar da fonte — e confirme visualmente que é a
mesma arte.

Para o fallback IMDb, a referência antiga continua válida e já foi conferida byte a
byte: `12-strong.jpg` tem que sair com exatos 46879 bytes por `._V1_QL75_UX400_`.

# ME AVISE NO FINAL

- Qualquer título ambíguo em que você teve que escolher — remake vs. original, ano
  diferente, título localizado, reboot/spin-off de nome parecido, filme de recap sem
  número de parte (a trilogia do Code Geass e os compilados de Made in Abyss / Madoka
  caem nisso) — para eu confirmar.
- **Todo título em que você não conseguiu arte limpa**, com o nome e qual tarja ou logo
  de plataforma sobrou, depois de esgotar as três tentativas da seção 3c.
- Todo título que ficou sem capa, e o motivo (não achei / travou o rate limit / só
  tinha arte tarjada e preferi perguntar).
- Toda capa cuja altura não saiu 600 (arte de proporção diferente de 2:3), com o
  número — é esperado, mas eu quero saber.
- Qualquer typo de título ou linha malformada que você notou no `.txt` — **sem
  corrigir o arquivo**.
- **Toda vez que a ordem dos cours for inferência sua** — sem pôster gêmeo com data e sem
  AniList, dizer qual arte você deu a qual linha e por quê, para eu conferir de olho.
- Todo anime que caiu no caso 3 do romaji (só existia arte com o **título traduzido** para
  o inglês).
- Qualquer **colisão de slug** nova que a regra do sufixo numérico não resolva — pergunte
  antes, não invente nome de arquivo.
- **Todo título cuja capa já estava no disco** e você só ligou ao `.txt` sem baixar, com o
  que você conferiu para aceitar a arte existente. E se você trocou alguma capa que já
  existia, diga qual e por quê — trocar arte instalada nunca é silencioso.
- **Se o CDN travou em algum momento**, quantas imagens tinham sido gastas até ali, em que
  cadência, e quanto tempo o poll levou para liberar. Esses números são o que mantém a seção
  de CADÊNCIA OBRIGATÓRIA calibrada.
