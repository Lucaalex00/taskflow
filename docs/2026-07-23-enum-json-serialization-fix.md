# 2026-07-23 — Fix enum JSON serialization mismatch between API and frontend

## Symptom

Reported while testing the app for real via `docker compose up --build`: creating a task
from the UI always failed. The browser's network tab (and nginx access logs) showed the
`POST /api/boards/{id}/tasks` call returning **400 Bad Request** with every field filled in.

## Root cause

`Program.cs` never configured `System.Text.Json` to serialize enums as strings. ASP.NET
Core's default is numeric (`TaskPriority.Medium` → `1`), but the Angular frontend sends and
expects the C# enum's string name (`"priority": "Medium"`), matching its own TypeScript enums
(`TaskState`, `TaskPriority`, `AlertSeverity`) which use string values by design (see
`core/models/*.model.ts`). Reproduced directly:

```
curl -X POST .../tasks -d '{"title":"Test task","priority":"Medium",...}'
→ 400: "$.priority": "The JSON value could not be converted to TaskFlow.Api.Controllers.CreateTaskRequest."
```

This wasn't just the create-task path — every enum field in every response (`TaskDto.State`,
`TaskDto.Priority`, `AlertDto.Severity`) would have serialized as a number too, silently
breaking every frontend comparison against its string-valued enums (Kanban column grouping,
valid-transition buttons, alert severity styling) the moment any of those endpoints returned
non-empty data. It went undetected until now because `TasksEndpointsTests` calls
`PostAsJsonAsync`/`GetFromJsonAsync` with .NET's own default `JsonSerializerOptions` on both
the test client and the server — two default numeric serializers exercising each other
consistently never surfaces a mismatch that only appears against a real string-sending client.

## Fix

`Api/Program.cs`:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

`tests/IntegrationTests/TasksEndpointsTests.cs` now serializes/deserializes its own requests
with a matching `JsonSerializerOptions` (`JsonStringEnumConverter` + `PropertyNameCaseInsensitive`,
mirroring the camelCase + string-enum wire format a real client sees), instead of .NET's
defaults — so the test suite actually exercises the format the frontend depends on, rather
than a same-process shortcut that happened to work.

## Verification

- `curl -X POST .../tasks -d '{"priority":"Medium",...}'` → `201 Created`; `GET .../tasks` now
  returns `"state":"Todo","priority":"Medium"` (strings) instead of numbers.
- `dotnet test`: 33 unit + 3 integration passing.
- Rebuilt and restarted the `api` container (`docker compose up -d --build api`) and confirmed
  the fix against the real container, not just `dotnet run`.
