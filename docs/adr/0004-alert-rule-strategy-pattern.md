# 0004 — Strategy pattern for alert rule evaluation

## Status
Accepted

## Context
The load-monitoring feature needs to support multiple, structurally different kinds of
threshold ("a user has too many overdue tasks", "a board's load spiked by X% in Y
minutes", "a user has too many tasks in progress at once") and the roadmap explicitly
anticipates more will be added later. A single method or switch statement evaluating all
rule types would grow indefinitely and become a shared point of failure.

## Decision
Define `AlertRuleType` as a Domain enum and `IAlertRuleEvaluator` as an Application-layer
interface (`RuleType` property + `EvaluateAsync`). Each rule type gets its own
`Infrastructure/Workers/AlertEvaluators/*Evaluator` implementation, injected as a
collection (`AddScoped<IAlertRuleEvaluator, ...>` registered once per evaluator) and
resolved by the worker via a dictionary keyed on `RuleType`.

## Rationale
- **Open/closed in practice**: adding a fourth rule type means adding one new class and
  one new DI registration line — `LoadMonitorWorker` itself never changes.
- **Each evaluator is independently unit-testable** with its own fake `ITaskFlowDbContext`,
  rather than one large method mixing three unrelated queries.
- **The worker stays generic**: `EvaluateBoardRulesAsync` loops over enabled rules and
  dispatches by type without knowing what any specific rule type actually checks.
- **A missing evaluator fails loudly but safely**: `LoadMonitorWorker` logs a warning and
  skips the rule rather than throwing and killing the whole monitoring cycle for every
  board.

## Alternatives considered
- **A big `switch` on `RuleType` inside the worker**: works for three rule types, but
  mixes unrelated query logic in one method and makes each rule type harder to unit test
  in isolation. Rejected specifically because the roadmap anticipates growth here.
- **Rules-as-data with a generic expression evaluator** (e.g. storing a JSON/DSL
  expression per rule): more flexible long-term, but adds real complexity (a mini
  expression language, sandboxing untrusted queries) that isn't justified for three
  well-known rule types in a portfolio project.

## Consequences
- Every new rule type requires touching two files: a new entry in the `AlertRuleType`
  enum (Domain) and a new evaluator class (Infrastructure) — a documented, repeatable
  recipe rather than an implicit convention.
- Alert deduplication (`WasRecentlyRaisedAsync` in `LoadMonitorWorker`) is handled once,
  generically, outside any individual evaluator — evaluators only decide *whether* a
  condition is met, never whether it was already reported.
