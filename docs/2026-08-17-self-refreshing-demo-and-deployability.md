# 2026-08-17 — A demo that refreshes itself, and images you can actually deploy

Follow-up to [the demo seed work](2026-08-14-demo-seed-configuration-and-personalization.md).
Seeding an empty database made the project worth looking at; this makes it stay that way
without supervision, and makes the published images deployable somewhere public.

## The problem with a demo you share

A seeded workspace is pristine exactly once. Anyone who visits can register an account, create
boards, drag cards around — and everything they leave behind is what the *next* person sees. For
a demo link that might be opened weeks apart (two interviews, two different weeks), "remember to
reset it beforehand" is a plan that fails the first time you're busy.

So the reset is now the instance's own job. `Seed:ResetIntervalHours` (168 — weekly — in the
Docker demo, `0` everywhere else) makes `DemoResetWorker` delete everything and re-seed once the
workspace has aged past the interval.

Two decisions worth writing down:

**The clock is the data's age, not a timer.** The obvious implementation — a `PeriodicTimer` set
to the reset interval — never fires on the hosts this is aimed at. Free tiers sleep after a few
minutes of inactivity, so a process started on Monday is very unlikely to still be running the
following Monday; it would wake, restart its weekly timer, and reset never. Instead the worker
polls every 15 minutes and compares `DateTime.UtcNow` against the demo owner's own
`CreatedAtUtc`, which the seeder stamps and the database persists across every restart. Waking
up after an eight-day nap now does the right thing immediately.

That also avoided a new table and a migration: the "when was this workspace built" timestamp
already existed, it just hadn't been read that way before.

**It refuses to run without a demo owner.** The reset is the only operation in the codebase that
deliberately destroys data, so it's guarded twice: the feature is off unless `Seed:Enabled` *and*
a positive interval are both configured, and even then it does nothing unless it finds
`demo@taskflow.dev` in the database. Point this build at a real database with the flag
accidentally left on and it declines rather than obliterates. The worker also logs a warning at
start-up when the reset is enabled, because a silent self-wiping database is the kind of thing
that should be impossible to enable by accident.

The delete order is explicit (alerts → metrics → rules → notifications → invitations → tasks →
members → boards → users): these entities reference each other by plain `Guid` rather than by
navigation properties, so EF can't infer the ordering, and real foreign keys will reject a guess.
That's precisely why `DemoWorkspaceResetterTests` runs against a Postgres container rather than
the in-memory provider — an in-memory pass would prove nothing about the constraints.

## Making the images deployable

`docker/nginx.conf` hardcoded `proxy_pass http://api:8080`, which is the API's hostname *on a
Docker Compose network* and nowhere else. The published frontend image therefore worked in
exactly one environment.

It's now `docker/default.conf.template`, dropped into `/etc/nginx/templates/`, where the nginx
image's entrypoint runs `envsubst` over it at container start. `API_UPSTREAM` defaults to
`api:8080`, so Compose behaves exactly as before, and a hosting platform sets its own internal
address without rebuilding anything. `NGINX_ENVSUBST_FILTER=^API_UPSTREAM$` keeps `envsubst`
away from nginx's own `$uri`, `$host` and `$http_upgrade`, which would otherwise be candidates
for substitution.

The topology is deliberately "frontend proxies the API": one public URL, one origin, no CORS,
and the JWT never travels to a second domain. Exposing the API directly instead would need a
frontend rebuild, because `apiUrl: '/api'` is compiled into the Angular production bundle —
that trade-off is written down in [`docs/deploying.md`](deploying.md) rather than left for
someone to discover.

## A GIF at the top of the README

Recruiters watch; they don't `docker compose up`. `e2e/capture-demo-frames.mjs` drives the real
app through the demo sign-in, the populated board, a drag between columns and the theme toggle,
grabbing frames on a timer while the interaction runs, so the animation shows a card actually
travelling rather than a series of end states. `build-demo-gif.py` assembles them with PIL —
no ffmpeg, since Pillow is already implied by nothing at all and is a smaller ask than a
system binary.

Two things keep it small enough to sit at the top of a README (1.7 MB): consecutive frames that
differ by less than 0.2% of their pixels collapse into a longer delay on the frame before them,
which turns the deliberate pauses into single frames; and every frame is quantised against one
shared palette, because per-frame palettes make flat UI surfaces shimmer between frames.

`make media` regenerates the animation and all five screenshots together.

## Verification

- 166 unit, **51** integration (two new: a fresh workspace is left alone, a stale one is wiped
  and rebuilt — including a visitor's account and board — and the rebuilt workspace is young
  enough that the next check is a no-op rather than a wipe-every-cycle loop), 137 frontend, 8 E2E.
- The E2E suite was re-run against a rebuilt frontend image, since the nginx change affects
  every request the browser makes.
- On the running stack: the rendered config inside the container shows `proxy_pass
  http://api:8080/api/` with nginx's own variables intact, `/api/config` and `/health` answer
  through the proxy, and the API logs the auto-reset warning at start-up.
- Screenshots and GIF were regenerated from a freshly seeded database, so they agree with each
  other and with what a first-time visitor sees.
