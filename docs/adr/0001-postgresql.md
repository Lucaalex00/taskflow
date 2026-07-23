# 0001 — PostgreSQL as the datastore

## Status
Accepted

## Context
TaskFlow needs a relational datastore for tasks, boards, users, alert rules, and a
time-series-like history of load snapshots (`LoadMetric`) queried by board and time range.
The roadmap's cost constraint rules out any managed database with a fixed monthly cost;
the project also needs to run entirely offline via `docker compose up` for demos.

## Decision
Use PostgreSQL, run as a Docker container in development/demo and accessed via
`Npgsql.EntityFrameworkCore.PostgreSQL`.

## Rationale
- **Zero fixed cost**: runs in a container locally; if a hosted demo is ever needed,
  Azure Database for PostgreSQL has a free-tier-eligible option, unlike SQL Server's
  licensing model for anything beyond the free Azure SQL tier.
- **Relational fit**: the domain is inherently relational (boards own tasks, alert
  rules reference boards, alerts reference rules) — no need for a document or
  graph model.
- **EF Core support is first-class** via Npgsql's provider, including enum-as-string
  conversions and index support used throughout `Infrastructure/Persistence/Configurations`.
- **Testcontainers has a mature Postgres module** (`Testcontainers.PostgreSql`), which is
  what makes real-database integration testing (see ADR 0002) practical without extra
  setup scripts.

## Consequences
- Local development requires Docker (already a hard requirement for the project's
  "zero deploy cost" principle, so this adds no new burden).
- Migrations are applied automatically on API startup (`Program.cs` calls
  `Database.MigrateAsync()`), keeping the one-command demo self-contained.
