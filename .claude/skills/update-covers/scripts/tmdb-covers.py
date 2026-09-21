#!/usr/bin/env python3
"""Triagem e instalacao de capas pela API do TMDB, para o watch-list-v3.

Versao de projeto do antigo ~/capas-tmdb.py, ja adaptada as regras do SKILL.md:
token v4 (Bearer), movie E tv (com temporada), download por `curl` (nao urllib),
cadencia de 1,5s com pausa de 3 min a cada 30 imagens, contador de imagens que
persiste entre sessoes, e slug ASCII igual a secao SLUG.

Uso (a ESCOLHA da arte continua sendo manual - olhe a contact sheet):

  ./tmdb-covers.py probe                       # o CDN esta liberado? (1 imagem)
  ./tmdb-covers.py validate                    # pipeline byte a byte, Fight Club (1 imagem)
  ./tmdb-covers.py search tv "Spartacus"       # nenhuma imagem gasta
  ./tmdb-covers.py info tv 46296               # seasons/episodios/pais, p/ desempate
  ./tmdb-covers.py posters tv 46296 --season 0 # metadados, nenhuma imagem gasta
  ./tmdb-covers.py triage tv 46296 spartacus --season 0 --n 6   # baixa w342 + sheet
  ./tmdb-covers.py install /abc123.jpg spartacus series         # 400px no repo
  ./tmdb-covers.py slug "Don't Look Up"
  ./tmdb-covers.py budget                      # imagens gastas na janela

Variaveis: CAPAS_DELAY (padrao 1.5), CAPAS_WORK (padrao ~/.cache/tmdb-covers/work),
CAPAS_PAUSE_EVERY (30), CAPAS_PAUSE (180).
"""
import json, os, re, subprocess, sys, time, urllib.error, urllib.parse, urllib.request

API = "https://api.themoviedb.org/3"
IMG = "https://image.tmdb.org/t/p"
TOKEN_FILE = os.path.expanduser("~/.config/tmdb/read_token")
STATE_DIR = os.path.expanduser("~/.cache/tmdb-covers")
STATE = os.path.join(STATE_DIR, "state.json")
WORK = os.environ.get("CAPAS_WORK", os.path.join(STATE_DIR, "work"))
DELAY = float(os.environ.get("CAPAS_DELAY", "1.5"))
PAUSE_EVERY = int(os.environ.get("CAPAS_PAUSE_EVERY", "30"))
PAUSE = int(os.environ.get("CAPAS_PAUSE", "180"))
AR_MIN, AR_MAX = 0.66, 0.70
FIGHT_CLUB = "/jSziioSwPVrOy9Yow3XhWIBDjq1.jpg"   # TMDB 550, referencia de validacao

# <repo>/.claude/skills/update-covers/scripts/tmdb-covers.py -> <repo>
REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), *[".."] * 4))
DIRS = {"movie": "images/movie", "series": "images/series", "anime": "images/anime"}


# ---------------------------------------------------------------- API (urllib)
def token():
    try:
        t = open(TOKEN_FILE).read().strip()
    except OSError:
        sys.exit(f"ERRO: token v4 nao encontrado em {TOKEN_FILE}")
    if not t:
        sys.exit(f"ERRO: {TOKEN_FILE} esta vazio")
    return t


def api(path, **params):
    """GET no api.themoviedb.org. urllib aqui e rapido - o problema e so no CDN."""
    url = f"{API}{path}"
    if params:
        url += "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={
        "Authorization": f"Bearer {token()}", "accept": "application/json"})
    for n in range(4):
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            if e.code == 429:
                wait = int(e.headers.get("Retry-After", 2 ** (n + 1)))
                print(f"  429 -> esperando {wait}s", file=sys.stderr)
                time.sleep(wait)
                continue
            raise
        except Exception:
            if n == 3:
                raise
            time.sleep(2 ** (n + 1))
    raise RuntimeError("falhou apos retries")


# ------------------------------------------------- orcamento do CDN (persiste)
def state_load():
    try:
        return json.load(open(STATE))
    except Exception:
        return {"images": []}


def state_add(n=1):
    os.makedirs(STATE_DIR, exist_ok=True)
    s = state_load()
    now = time.time()
    s["images"] = [t for t in s["images"] if now - t < 6 * 3600] + [now] * n
    json.dump(s, open(STATE, "w"))
    return len([t for t in s["images"] if now - t < 3600])


def budget():
    now = time.time()
    imgs = state_load()["images"]
    for label, window in (("10 min", 600), ("1 h", 3600), ("6 h", 21600)):
        print(f"  imagens nos ultimos {label}: {len([t for t in imgs if now - t < window])}")
    if imgs:
        print(f"  ultima imagem ha {int(now - max(imgs))}s")
    print("  teto de planejamento: ~85 por janela (ver CADENCIA OBRIGATORIA no SKILL.md)")


# --------------------------------------------------------- download com `curl`
def fetch(url, dest):
    """Baixa via curl (urllib custa ~30s por imagem nesta maquina). True se ok."""
    os.makedirs(os.path.dirname(os.path.abspath(dest)), exist_ok=True)
    tmp = dest + ".part"
    r = subprocess.run(["curl", "-s", "-o", tmp, "-w", "%{http_code}",
                        "--max-time", "30", url], capture_output=True, text=True)
    code = r.stdout.strip()
    ok = code == "200" and os.path.exists(tmp) and os.path.getsize(tmp) > 0
    if ok:
        os.replace(tmp, dest)
    elif os.path.exists(tmp):
        os.remove(tmp)
    spent = state_add()
    if not ok:
        print(f"  !! FALHOU http={code} ({'BLOQUEIO do CDN' if code == '000' else 'erro'}) {url}",
              file=sys.stderr)
    if spent % PAUSE_EVERY == 0:
        print(f"  -- {spent} imagens na janela: pausa de {PAUSE}s", file=sys.stderr)
        time.sleep(PAUSE)
    else:
        time.sleep(DELAY)
    return ok


def probe():
    r = subprocess.run(["curl", "-s", "-o", "/dev/null",
                        "-w", "code=%{http_code} conn=%{time_connect} total=%{time_total}",
                        "--max-time", "40", f"{IMG}/w342{FIGHT_CLUB}"],
                       capture_output=True, text=True)
    state_add()
    print(r.stdout)
    print("  code=200 e total<1s -> CDN liberado. code=000 -> bloqueio, faca poll de 1/min.")


def validate():
    os.makedirs(WORK, exist_ok=True)
    orig = os.path.join(WORK, "_validate_fight_club.jpg")
    out = os.path.join(WORK, "fight-club.jpg")
    if not fetch(f"{IMG}/original{FIGHT_CLUB}", orig):
        sys.exit("validacao abortada: nao baixou o original")
    convert(orig, out)
    size = os.path.getsize(out)
    ref = os.path.join(REPO, "WatchList/wwwroot/images/movie/fight-club.jpg")
    same = subprocess.run(["cmp", "-s", out, ref]).returncode == 0 if os.path.exists(ref) else None
    print(f"  {out}  {size} bytes (esperado 47790)  identico ao do repo: {same}")
    if size != 47790:
        print("  !! diferenca: suspeite da versao do ImageMagick antes da fonte")


# ------------------------------------------------------------------ utilidades
def convert(src, dest):
    exe = "convert" if subprocess.run(["which", "convert"], capture_output=True).returncode == 0 else "magick"
    subprocess.run([exe, src, "-resize", "400x", "-quality", "82", dest], check=True)


SLUG_RE = re.compile(r"^[a-z0-9][a-z0-9-]*$")
PATH_RE = re.compile(r"^/[A-Za-z0-9._-]+\.(jpg|png)$")
SIZES = ("original", "w780", "w342")


def check(kind=None, tid=None, season=None, slug=None, file_path=None, size=None):
    """Valida o que vem da linha de comando antes de virar URL ou caminho de arquivo."""
    if kind is not None and kind not in ("movie", "tv"):
        sys.exit("ERRO: tipo deve ser movie ou tv")
    if tid is not None and not str(tid).isdigit():
        sys.exit("ERRO: id do TMDB deve ser numerico")
    if season is not None and not str(season).isdigit():
        sys.exit("ERRO: --season deve ser numerico")
    if slug is not None and not SLUG_RE.match(slug):
        sys.exit("ERRO: slug fora da regra [a-z0-9-] (ver secao SLUG)")
    if file_path is not None and not PATH_RE.match(file_path):
        sys.exit("ERRO: file_path deve ser o do TMDB, no formato /xxxx.jpg")
    if size is not None and size not in SIZES:
        sys.exit(f"ERRO: --size deve ser um de {', '.join(SIZES)}")


def slugify(t):
    """Igual a secao SLUG: apostrofo (e crase) somem, o resto vira hifen, ASCII."""
    s = t.lower()
    for ch in ("'", "`", "’", "´"):
        s = s.replace(ch, "")
    s = "".join(c if (c.isalnum() and c.isascii()) else "-" for c in s)
    while "--" in s:
        s = s.replace("--", "-")
    return s.strip("-")


def posters_of(kind, tid, season=None, all_langs=False):
    check(kind=kind, tid=tid, season=season)
    path = f"/{kind}/{tid}/images" if season is None else f"/{kind}/{tid}/season/{season}/images"
    params = {} if all_langs else {"include_image_language": "en,null"}
    data = api(path, **params)
    ps = [p for p in data.get("posters", []) if AR_MIN <= p.get("aspect_ratio", 0) <= AR_MAX]
    if not ps and not all_langs:
        print("   (nenhum poster em en,null na proporcao; repetindo sem filtro de idioma)")
        return posters_of(kind, tid, season, all_langs=True)
    return ps


def show(ps):
    for i, p in enumerate(ps, 1):
        print(f"   {i:2}. {p['file_path']}  {p['width']}x{p['height']}  "
              f"ar={p['aspect_ratio']:.3f}  votos={p['vote_count']}  lang={p.get('iso_639_1')}")


# -------------------------------------------------------------------- comandos
def cmd_search(kind, query, year=None):
    check(kind=kind)
    if year is not None and not str(year).isdigit():
        sys.exit("ERRO: --year deve ser numerico")
    params = {"query": query}
    if year:
        params["primary_release_year" if kind == "movie" else "first_air_date_year"] = year
    for m in api(f"/search/{kind}", **params).get("results", [])[:8]:
        title = m.get("title") or m.get("name")
        orig = m.get("original_title") or m.get("original_name")
        date = (m.get("release_date") or m.get("first_air_date") or "?")[:4]
        print(f"  [{m['id']:>8}] {title} ({date})  orig={orig}")


def cmd_info(kind, tid):
    check(kind=kind, tid=tid)
    d = api(f"/{kind}/{tid}")
    title = d.get("title") or d.get("name")
    print(f"  [{tid}] {title}  orig={d.get('original_title') or d.get('original_name')}")
    print(f"  pais={d.get('origin_country')}  lang={d.get('original_language')}  "
          f"data={d.get('release_date') or d.get('first_air_date')}")
    if kind == "tv":
        print(f"  temporadas={d.get('number_of_seasons')}  episodios={d.get('number_of_episodes')}")
        for s in d.get("seasons", []):
            print(f"    T{s['season_number']}: {s['name']}  eps={s['episode_count']}  "
                  f"{(s.get('air_date') or '?')[:4]}")


def cmd_triage(kind, tid, slug, season=None, n=6):
    check(kind=kind, tid=tid, season=season, slug=slug)
    ps = posters_of(kind, tid, season)[:n]
    show(ps)
    os.makedirs(WORK, exist_ok=True)
    files = []
    for i, p in enumerate(ps, 1):
        dest = os.path.join(WORK, f"{slug}--{i}.jpg")
        if fetch(f"{IMG}/w342{p['file_path']}", dest):
            files.append((i, dest))
    if files:
        sheet = os.path.join(WORK, f"{slug}--sheet.jpg")
        args = []
        for i, f in files:
            args += ["-label", str(i), f]
        subprocess.run(["montage", *args, "-resize", "200x", "-tile", "4x",
                        "-geometry", "+5+5", sheet], check=True)
        print(f"   sheet: {sheet}   ({len(files)}/{len(ps)} candidatos)")


def cmd_install(file_path, slug, lista, size="original", force=False):
    if lista not in DIRS:
        sys.exit(f"ERRO: lista deve ser {'/'.join(DIRS)}")
    check(slug=slug, file_path=file_path, size=size)
    imgdir = os.path.join(REPO, "WatchList/wwwroot", DIRS[lista])
    dest = os.path.join(imgdir, f"{slug}.jpg")
    if os.path.dirname(os.path.realpath(dest)) != os.path.realpath(imgdir):
        sys.exit("ERRO: destino escaparia do diretorio de imagens")
    if os.path.exists(dest) and not force:
        sys.exit(f"ERRO: {dest} ja existe. Trocar arte instalada nunca e silencioso - "
                 "confirme com o Denis e use --force.")
    os.makedirs(WORK, exist_ok=True)
    orig = os.path.join(WORK, f"_{slug}_orig.jpg")
    if not fetch(f"{IMG}/{size}{file_path}", orig):
        sys.exit("instalacao abortada: nao baixou o original")
    convert(orig, dest)
    dim = subprocess.run(["identify", "-format", "%wx%h %b", dest],
                         capture_output=True, text=True).stdout
    print(f"  instalado: {dest}  {dim}")
    if not dim.startswith("400x600"):
        print("  !! altura diferente de 600 (arte fora de 2:3) - reporte no final")


def main(argv):
    if len(argv) < 2:
        sys.exit(__doc__)
    cmd, a = argv[1], argv[2:]
    opt = lambda name, default=None: (a[a.index(name) + 1] if name in a else default)
    pos = [x for i, x in enumerate(a)
           if not x.startswith("--") and (i == 0 or not a[i - 1].startswith("--"))]
    if cmd == "probe":
        probe()
    elif cmd == "validate":
        validate()
    elif cmd == "budget":
        budget()
    elif cmd == "slug":
        print(slugify(pos[0]))
    elif cmd == "search":
        cmd_search(pos[0], pos[1], opt("--year"))
    elif cmd == "info":
        cmd_info(pos[0], pos[1])
    elif cmd == "posters":
        show(posters_of(pos[0], pos[1], opt("--season"), "--all-langs" in a))
    elif cmd == "triage":
        cmd_triage(pos[0], pos[1], pos[2], opt("--season"), int(opt("--n", "6")))
    elif cmd == "install":
        cmd_install(pos[0], pos[1], pos[2], opt("--size", "original"), "--force" in a)
    else:
        sys.exit(__doc__)


if __name__ == "__main__":
    main(sys.argv)
