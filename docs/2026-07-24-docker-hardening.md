# 2026-07-24 — Harden the Docker setup: non-root frontend, restart policies, resource limits, image scanning

## Context

Reviewing the project from a "would a Docker-focused technical reviewer notice gaps" angle
surfaced a few real ones: the frontend container ran nginx as root (the API container already
ran as a dedicated non-root user), no service had a restart policy, nothing bounded container
resource usage, and CI never scanned the published images for known vulnerabilities.

## What changed

- **Non-root frontend container**: switched `Dockerfile.frontend`'s runtime stage from
  `nginx:1.27-alpine` to `nginxinc/nginx-unprivileged:1.27-alpine` — the same nginx, but
  pre-configured with writable temp/cache directories and a default non-root user, so it can
  listen on an unprivileged port without a root process anywhere in the container. Moved the
  listen port from 80 to 8080 accordingly (`docker/nginx.conf`, `Dockerfile.frontend`'s
  `EXPOSE`/healthcheck, `docker-compose.yml`'s port mapping — the host-side port stays 4200,
  only the container-internal port changed).
- **Restart policies**: `restart: unless-stopped` on all three services in
  `docker-compose.yml` — previously a crashed container (e.g. Postgres OOM-killed) would just
  stay down until someone noticed and ran `docker compose up` again.
- **Resource limits**: `deploy.resources.limits` (cpus/memory) on all three services — modern
  `docker compose up` (Compose V2, not swarm) honors these directly. Without them, a bug in any
  one container (e.g. a memory leak) could exhaust the host and take down its neighbors too.
- **Vulnerability scanning in CI**: the `docker` job now runs Trivy against both published
  images (API and frontend) after pushing them to GHCR, reporting CRITICAL/HIGH CVEs (OS
  packages + .NET/npm dependencies) to the repo's Security tab via SARIF upload. Deliberately
  non-blocking (`exit-code: '0'`) — a demo project shouldn't go red over a CVE in an upstream
  base image nobody's shipped a fix for yet, but it should still be visible to whoever's
  reviewing the repo.

## Verification

- `docker compose up --build -d`, then:
  - `docker exec taskflow-frontend whoami` → `nginx` (previously would have been `root`)
  - `docker inspect taskflow-frontend --format '{{.HostConfig.RestartPolicy.Name}}'` →
    `unless-stopped`
  - `docker inspect taskflow-frontend --format '{{.HostConfig.Memory}} {{.HostConfig.NanoCpus}}'`
    → non-zero, matching the configured limits
  - Full smoke test through the proxy: `curl -X POST http://localhost:4200/api/users ...` →
    `201 Created` with a JWT, confirming the port change (80 → 8080 internally) didn't break
    the nginx → API proxy path.
- `dotnet test` / `ng test` unaffected (no application code changed).
