# Comandi — guida di riferimento

Tutti i comandi usati per far girare, ispezionare e ripulire lo stack Docker di TaskFlow,
più le scorciatoie `make` che li avvolgono. Vedi [`explanation.md`](explanation.md) per il
"perché" dietro ogni scelta architetturale citata qui.

---

## Avviare lo stack

### Build da zero (compila davvero .NET + Angular)
```bash
docker compose up --build -d
```
- `up` = crea e avvia i container definiti in `docker-compose.yml`.
- `--build` = ricostruisce le immagini prima di avviare, invece di riusare quelle già
  costruite in precedenza. Serve ogni volta che cambi codice sorgente (backend o
  frontend) — senza `--build`, Docker riuserebbe l'immagine vecchia e non vedresti le
  tue modifiche.
- `-d` = *detached*, cioè in background. Senza, il terminale resta agganciato ai log di
  tutti i container e si blocca finché non premi Ctrl+C (che spegne anche lo stack).

Equivalente breve: `make up`.

### Immagini già pronte (nessuna build, scarica da GHCR)
```bash
docker compose -f docker-compose.yml -f docker-compose.prebuilt.yml up -d
```
- `-f file1 -f file2` = usa più file di configurazione insieme. Compose li unisce nel
  ordine in cui li elenchi: il secondo file sovrascrive solo i pezzi che definisce (qui,
  solo `image:` al posto di `build:`), il resto (rete, `depends_on`, variabili) resta
  quello del primo file.
- Non serve `--build`: `docker-compose.prebuilt.yml` dice esplicitamente a Compose di
  scaricare (`pull_policy: always`) l'immagine da `ghcr.io/lucaalex00/taskflow`, non di
  costruirla.

Equivalente breve: `make demo`.

### Su che porta è finito il frontend?
```bash
docker compose port frontend 8080
```
`docker-compose.yml` non fissa più una porta host per il frontend (`ports: - "${WEB_PORT:-}:8080"`
senza un valore di default): se non imposti `WEB_PORT` in `.env`, Docker sceglie da solo la prima
porta libera sulla macchina, per non entrare in conflitto con un altro progetto già in ascolto su
4200. Questo comando stampa quella scelta (es. `0.0.0.0:4200`, ma può essere un'altra). `make up`
e `make demo` lo stampano già in automatico dopo l'avvio.

### Con un file `.env` personalizzato
```bash
cp .env.example .env      # una tantum, poi modifica i valori che vuoi
docker compose up --build -d
```
Compose legge automaticamente un file chiamato `.env` nella stessa cartella e sostituisce
i placeholder `${VAR:-default}` nei file YAML. Se non esiste `.env`, vengono usati i
default già scritti in `docker-compose.yml` — per questo il comando funziona anche a
`.env` non creato.

Equivalente breve: `make env` (crea `.env` da `.env.example` solo se non esiste già).

---

## Ispezionare lo stack mentre gira

```bash
docker compose ps
```
Elenca i container di questo progetto, il loro stato (`Up`, `Exited`...) e — importante —
se l'healthcheck li segna `healthy` o `unhealthy`. Primo comando da lanciare se qualcosa
non risponde: ti dice subito quale dei tre servizi non è partito bene.

```bash
docker compose logs -f api
docker compose logs -f frontend
docker compose logs -f postgres
```
`-f` = *follow*, streamma i log in tempo reale (come `tail -f`). Senza servizio specificato
(`docker compose logs -f`) segui i log di **tutti** i servizi insieme, interlacciati.
Usalo quando un container si riavvia in loop o un healthcheck resta `unhealthy` — il log
dice quasi sempre perché.

Equivalente breve: `make logs` (segue solo l'API).

```bash
docker compose exec api sh
```
Apre una shell **dentro** il container `api` già in esecuzione. Utile per controllare a
mano che una variabile d'ambiente sia arrivata correttamente, o per lanciare un comando
diagnostico dall'interno. `exec` diverso da `run`: `exec` entra in un container già
acceso, `run` ne accenderebbe uno nuovo temporaneo.

```bash
docker stats
```
Mostra CPU/RAM in tempo reale per ogni container attivo — utile per verificare che i
limiti impostati in `docker-compose.yml` (`deploy.resources.limits`) abbiano senso.

---

## Fermare e ripulire

```bash
docker compose down
```
Ferma e rimuove i container e la rete creata da Compose. **Non tocca i volumi** — i dati
del database restano salvati per il prossimo avvio.

Equivalente breve: `make down`.

```bash
docker compose down -v
```
Come sopra, ma `-v` rimuove **anche i volumi** — cancella davvero i dati del database
Postgres. Al prossimo `up`, il database riparte vuoto e viene ri-seedato con i dati demo
finti (se `SEED_DEMO=true`). Usalo quando vuoi ripartire da uno stato pulito, non quando
vuoi solo mettere in pausa lo stack.

Equivalente breve: `make reset`.

```bash
docker system prune
```
**Non specifico a questo progetto** — ripulisce immagini, container fermi e reti
inutilizzate su tutto Docker, non solo TaskFlow. Utile ogni tanto per liberare spazio
disco, da usare con consapevolezza perché tocca anche altri progetti Docker sulla stessa
macchina.

---

## Test (alcuni richiedono Docker attivo, altri no)

```bash
dotnet test
```
Esegue sia gli unit test sia gli integration test del backend. Gli integration test
avviano un **vero container Postgres temporaneo** al volo tramite Testcontainers — per
questo serve Docker Desktop acceso anche solo per lanciare questo comando, anche se non
hai fatto tu `docker compose up`.

Equivalente breve: `make test-backend`.

```bash
cd frontend && npx ng test --watch=false --browsers=ChromeHeadless
```
Test unitari Angular (Karma/Jasmine), headless — non serve Docker.

Equivalente breve: `make test-frontend`.

```bash
docker compose -f docker-compose.yml -f docker-compose.e2e.yml up --build -d
cd e2e && npx playwright test
```
Fa partire lo stack reale (variante pensata per i test E2E) e ci lancia contro dei test
Playwright che pilotano un browser vero — click, drag & drop, form — esattamente come
farebbe una persona. Questo è il livello di test più vicino a "l'utente reale usa l'app".

Equivalente breve: `make test-e2e`.

---

## Le scorciatoie `make` — tabella riassuntiva

Il [`Makefile`](Makefile) è solo un elenco di alias per non riscrivere i comandi sopra
per intero ogni volta. Su Windows richiede Git Bash o WSL (non funziona nel Prompt dei
comandi/PowerShell nativo).

| Comando        | Cosa fa |
|---|---|
| `make up`      | Build da zero + avvio (`docker compose up --build -d`) |
| `make demo`    | Avvio da immagini già pronte su GHCR, nessuna build |
| `make down`    | Ferma lo stack, tiene i dati del database |
| `make reset`   | Ferma lo stack e cancella anche i dati (`-v`) |
| `make logs`    | Segue i log dell'API in tempo reale |
| `make build`   | Compila backend e frontend **senza Docker**, in locale |
| `make test`    | Esegue sia i test backend sia quelli frontend |
| `make test-e2e`| Avvia lo stack e lancia i test Playwright contro il browser reale |
| `make env`     | Crea `.env` da `.env.example` (non sovrascrive se già esiste) |
| `make lint`    | Lint Angular |

`make help` stampa questa stessa lista direttamente dal Makefile, quindi resta sempre
aggiornata anche se questo file non viene toccato per un po'.
