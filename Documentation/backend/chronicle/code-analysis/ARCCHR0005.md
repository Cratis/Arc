# ARCCHR0005: Chronicle is used but not wired up

## Rule

Running Arc **without** Chronicle is a valid, supported setup — `AddCratisArc` on its own backs commands and queries with MongoDB or EF Core and never needs an event store. So a missing `WithChronicle()` is only a problem when the project actually **uses** Chronicle. This rule fires when a project calls `AddCratisArc` (without `WithChronicle()` or `AddCratis()`) yet does one of the following in the same project:

- declares an **aggregate root**;
- declares a **reactor** or a **reducer**;
- declares a **projection** (`IProjectionFor<>`);
- declares an **`[EventType]`** event;
- injects a Chronicle service such as **`IEventLog`** or **`IEventStore`** (appending events, returning events from a command handler, reading the event store).

In any of those cases the event store is required, so a command, query, reactor, or reducer that touches Chronicle fails to resolve at runtime.

The rule only reports when the setup call and the Chronicle usage live in the **same project**. When Arc is set up in a separate host project, it stays silent — and when the project genuinely doesn't use Chronicle, it never fires.

## Severity

Warning

## Example

### Violation

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record AuthorRegistered(string Name);

var builder = WebApplication.CreateBuilder(args);

// ARCCHR0005: Chronicle artifacts exist, but Chronicle is never wired up
builder.AddCratisArc();

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

### Fix

Add the event store with `WithChronicle()` on the Arc builder:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithChronicle());

var app = builder.Build();
app.UseCratisArc();
app.UseCratisChronicle();
app.Run();
```

Or use the all-in-one `AddCratis()`, which wires Arc, the Chronicle client, and identity together:

```csharp
builder.AddCratis();

var app = builder.Build();
app.UseCratis();
app.Run();
```

## Why This Rule Exists

`AddCratisArc` deliberately supports [running Arc without an event store](../../../arc-without-event-sourcing.md), backed by MongoDB or EF Core. That flexibility means the framework cannot assume Chronicle is wanted — so forgetting `WithChronicle()` is a silent mistake that only surfaces the first time an event is appended or read. This rule catches it at compile time, before the application runs.

## Related Rules

- None
