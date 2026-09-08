# Come funziona TaskFlow — spiegazione end-to-end

Questo file spiega, con esempi concreti presi dai file veri di questo repo, come Docker
impacchetta il progetto e come i tre servizi (database, API, frontend) lavorano insieme.
Se stai leggendo questo perché vuoi capire il progetto e non solo farlo funzionare, questo
è il documento giusto.

---

## 1. Il vocabolario, con l'analogia del vaso

- **Immagine** = la ricetta congelata. Non gira, è solo un insieme di file + istruzioni
  ("parti da questo sistema base, copia questi file, esegui questo comando all'avvio").
- **Container** = un'immagine mandata in esecuzione. È il "vaso" pronto e vivo.
- **Dockerfile** = il testo della ricetta. In questo repo ce ne sono due:
  [`Dockerfile.api`](Dockerfile.api) e [`Dockerfile.frontend`](Dockerfile.frontend).
- **docker-compose.yml** = l'istruzione che dice "accendi più vasi insieme, in questo ordine,
  e falli parlare tra loro". Non costruisce nulla da solo, orchestra.

La tua immagine del "vaso con tutto dentro" è corretta: un container si porta dietro
l'intero ambiente di cui ha bisogno (runtime, librerie, permessi) — non dipende da cosa è
installato sulla macchina che lo fa girare.

---

## 2. Due concetti diversi che sembrano uno solo (la correzione importante)

Nel tuo riassunto hai unito due cose che nel progetto sono **separate e indipendenti**:

### A. Gli "strati" dentro un singolo Dockerfile (build-time, dentro un solo vaso)

Guarda [`Dockerfile.api`](Dockerfile.api):

```dockerfile
# --- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build   # stage 1: ha il compilatore .NET
...
RUN dotnet publish ... -o /app/publish            # compila il codice

# --- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime   # stage 2: parte da zero
...
COPY --from=build /app/publish .                  # copia SOLO il risultato compilato
```

Questi sono i due "strati" (si chiamano **stage**, build multi-stage). Non è che uno "si
accende prima" a runtime — succedono entrambi **durante la build dell'immagine**, prima
ancora che il container esista. Lo stage 1 (`build`) usa un'immagine pesante con il
compilatore, produce il binario, e poi viene **buttato via**. Lo stage 2 (`runtime`) è
l'unico che resta nell'immagine finale, e contiene solo il binario compilato — non il
compilatore, non i sorgenti .cs, non i tool di build.

Perché farlo così: se non separassi gli stage, l'immagine che gira in produzione si
porterebbe dietro il compilatore .NET intero (centinaia di MB in più) e i sorgenti — più
lenta da scaricare, più superficie d'attacco se qualcuno entra nel container. Con gli
stage separati, l'immagine finale contiene solo ciò che serve per *eseguire*, non per
*costruire*.

Stesso schema in [`Dockerfile.frontend`](Dockerfile.frontend): stage 1 usa Node per
compilare Angular (`npm run build`), stage 2 butta via Node e usa solo nginx per servire
i file statici già pronti (HTML/CSS/JS compilati).

### B. L'ordine di avvio tra servizi diversi (runtime, tra vasi diversi)

Questo invece è nel [`docker-compose.yml`](docker-compose.yml), ed è ciò che
effettivamente corrisponde alla tua immagine di "il vaso più in alto parte prima, gli
altri aspettano che sia verde":

```yaml
api:
  depends_on:
    postgres:
      condition: service_healthy   # l'API NON parte finché postgres non risponde "pronto"

frontend:
  depends_on:
    api:
      condition: service_healthy   # il frontend NON parte finché l'API non risponde "pronto"
```

Ordine reale all'avvio: **postgres → api → frontend**. Ogni servizio ha un
`healthcheck` (un comando che Docker ripete ogni pochi secondi per chiedere "sei pronto
davvero, non solo acceso?"):

```yaml
# postgres, in docker-compose.yml
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U taskflow -d taskflow"]
  interval: 5s
```

```dockerfile
# api, in Dockerfile.api
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1
```

**Perché serve davvero, non è burocrazia**: se l'API partisse subito insieme a postgres,
proverebbe a connettersi a un database che magari sta ancora inizializzando i propri file
interni — e crasherebbe al primo avvio, ogni volta, in modo intermittente e fastidioso da
diagnosticare. Con `depends_on: condition: service_healthy`, Docker aspetta il verde vero
prima di accendere il pezzo successivo.

**Riassunto della correzione**: gli "stage" (A) riguardano *come viene costruita
un'immagine*, uno alla volta, prima che esista qualunque container. L'ordine di avvio (B)
riguarda *come vengono accesi più container diversi*, uno che aspetta l'altro. Sono due
meccanismi Docker diversi che nel tuo riassunto erano diventati una cosa sola — nel
progetto convivono entrambi ma non si toccano tra loro.

---

## 3. I tre servizi e come si parlano

```
postgres  (database, immagine già pronta, non costruita da noi)
   ↑ aspettato da
api       (backend .NET, costruito da Dockerfile.api)
   ↑ aspettato da
frontend  (Angular + nginx, costruito da Dockerfile.frontend)
```

Punto chiave che sorprende chi vede Docker per la prima volta: dentro
`docker-compose.yml`, l'API si connette al database così:

```yaml
ConnectionStrings__Postgres: "Host=postgres;Port=5432;..."
```

`postgres` **non è un indirizzo IP**, è il *nome del servizio* scritto sopra nello stesso
file. Docker Compose crea automaticamente una piccola rete privata interna e fa in modo
che ogni servizio possa raggiungere gli altri chiamandoli per nome, come se fosse un DNS
locale. Stessa cosa per il frontend: nginx inoltra le chiamate `/api/...` verso
`api:8080` — di nuovo, un nome di servizio, non un IP scritto a mano.

Questo è anche il motivo per cui, da fuori (dal tuo browser), parli solo con il frontend
(nginx) — es. `localhost:4200`, ma la porta host esatta dipende da cosa era libero sulla
tua macchina al momento dell'avvio (vedi il riquadro più sotto): il browser non parla mai
direttamente con l'API. Parla con nginx (il frontend), e **nginx** inoltra la richiesta
all'API dentro la rete privata Docker.
nginx fa quindi due lavori insieme: (1) serve i file statici di Angular, (2) fa da
reverse proxy verso l'API. Guarda [`docker/default.conf.template`](docker/default.conf.template)
per la configurazione di quel proxy.

---

## 4. Build da zero vs. immagini già pronte

Hai due modi per far partire lo stack, e corrispondono esattamente alla tua distinzione
"costruisco il vaso da zero" vs. "prendo il vaso già fatto":

**Build da zero** (compila davvero .NET e Angular sulla tua macchina, richiede minuti):
```bash
docker compose up --build -d
```
Usa `Dockerfile.api` e `Dockerfile.frontend` per costruire le immagini localmente.

**Immagini già pronte** (scarica il risultato già compilato, richiede secondi):
```bash
docker compose -f docker-compose.yml -f docker-compose.prebuilt.yml up -d
```
Il file [`docker-compose.prebuilt.yml`](docker-compose.prebuilt.yml) sovrascrive solo il
pezzo `build:` con `image: ghcr.io/lucaalex00/taskflow/api:latest` — cioè "non
costruire niente, scarica l'immagine che la CI ha già pubblicato l'ultima volta che i
test sono passati". Sono le stesse identiche immagini, prodotte dalla pipeline GitHub
Actions ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) ad ogni push verde su
`main`.

In entrambi i casi, all'avvio l'API applica automaticamente le migration del database e
— se il database è vuoto — inserisce dati demo finti (utenti, board, task) così l'app è
subito esplorabile senza doverla configurare a mano. Questo è controllato da
`Seed__Enabled: true` in `docker-compose.yml`.

**Nota sulla porta del frontend**: in `docker-compose.yml` la riga `ports:` del servizio
`frontend` è `"${WEB_PORT:-}:8080"` — senza un default dopo i due punti. Se non imposti
`WEB_PORT` in `.env`, quella variabile è vuota e Docker interpreta "nessuna porta host
richiesta esplicitamente", quindi ne assegna una libera da solo. È lo stesso meccanismo di
`docker run -p 8080` (senza specificare la porta host): comodo perché evita che due
progetti Docker sulla stessa macchina litighino per la 4200, ma significa che l'URL non è
sempre lo stesso a ogni avvio. Per saperlo: `docker compose port frontend 8080` (vedi
[`Commands.md`](Commands.md)).

---

## 5. Perché tutto questo esiste (il "perché" dietro la containerizzazione)

La tua conclusione è giusta, la riformulo solo per essere precisa:

Senza Docker, chi vuole provare il tuo progetto deve: installare .NET nella versione
giusta, installare Node nella versione giusta, installare Postgres, creare un database,
copiare `.env.example` in `.env` e configurarlo a mano, girare le migration, e sperare che
nessuna versione installata sulla sua macchina confligga con quella richiesta dal
progetto. Questo è esattamente il problema che GitHub da solo non risolve: `git clone`
ti dà i sorgenti, non l'ambiente per farli girare.

Con Docker, ogni pezzo (database, backend, frontend) porta con sé il proprio ambiente
completo e isolato. Un `docker compose up` basta perché non serve installare nulla
sull'host: Docker scarica o costruisce ogni immagine, e ogni container è autosufficiente.
Questo vale sia per un recruiter che vuole vedere il progetto funzionare in un click, sia
per la CI (che ha bisogno esattamente dello stesso ambiente ogni volta, senza sorprese
dovute a "sulla macchina del runner manca qualcosa"), sia per te che vuoi sviluppare
senza inquinare il tuo sistema con dieci versioni diverse di Node installate nel tempo.

E la tua ultima osservazione è corretta: una volta che hai il "vaso" già pronto e
funzionante, personalizzarlo (cambiare una regola di business, aggiungere una feature)
significa mettere le mani nei sorgenti dentro quel vaso — a quel punto il progetto
smette di essere "il progetto demo di qualcun altro" e diventa il tuo.
