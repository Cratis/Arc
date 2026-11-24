---
title: Suspending parts
description: Suspend and resume a module, feature, slice or part for a tenant, a user or the system without declaring anything, and feed suspensions from your own state.
---

An operator can suspend any part of the application for a tenant, a user or the system, and resume it later. The model does not
declare anything for this: every discovered module, feature, slice, command and read model can be suspended. A suspended part
is refused with the reason `Suspended`, which is distinct from `NotAvailable`.

Audiences are **granted and withdrawn**. Parts are **suspended and resumed**.

## What a suspension does

- A suspension is checked first: before gates, so before audiences and context values are resolved, and before `authorize`.
- A suspension at any level wins over every grant. Deny wins.
- Suspending a module or a feature suspends every slice and part under it.
- A command or query in a suspended part is refused with `AuthorizationFailureReason` `Suspended` (`AvailabilityReason.Suspended`) and HTTP status `403`, the same status as `NotAvailable`, so a client tells the two apart by the reason.
- `/.cratis/availability` lists each part that is refused together with its `reason`, `NotAvailable` or `Suspended`.
- The MCP tools of a suspended part are left out of the tool listing.
- Suspending and resuming reach open sessions: the gate subscribes to the suspension source, and each change makes the session decide again from current state.

## Part paths

A suspension names a part with a path. A path is the slice location joined with `/`, leaving out what the application does not
have, and for a part followed by the full name of its type:

| Level | Path |
| --- | --- |
| Module | `Fleet` |
| Feature | `Fleet/DataQuality` |
| Slice | `Fleet/DataQuality/QualityScores` |
| Part | `Fleet/DataQuality/QualityScores/Fleet.DataQuality.QualityScores.QualityScores` |

A path covers itself and everything below it. `PartPaths.For(type)` gives the path of a part, and
`DiscoveredParts.Entries()` lists every module, feature, slice and part found from the types, so an operator can pick one.
`AvailabilityManifest.Parts` carries the same entries (`PartEntry` with `Kind`, `Path` and `Name`).

## Levels

| Level | Applies to |
| --- | --- |
| `Tenant` | Callers of the tenant named by the key. |
| `User` | The user named by the key. |
| `System` | The system caller only. |

The newest decision wins per level and path: a resume that is newer than a suspension at the same level and path ends it. A
suspension at any other level still holds.

## Feed suspensions from your own state

`ISuspensionSource` is the contract a product implements, beside `IAudienceSource`:

```csharp
public interface ISuspensionSource
{
    ValueTask<IReadOnlyCollection<PartSuspensionDecision>> GetDecisions(AudienceSubject subject, CancellationToken cancellationToken = default);
    IDisposable OnChanged(Func<SuspensionChange, ValueTask> handler);
}

public record PartSuspensionDecision(string Path, AudienceLevel Level, bool IsSuspended, DateTimeOffset DecidedAt);
```

- `GetDecisions` returns the decisions that apply to the subject: those at the tenant level for its tenant, at the user level for its user, and at the system level when the subject is the system. Return resumes as well as suspensions, in any order; the engine keeps the newest per level and path.
- `OnChanged` must call the handler after every suspend or resume, so open sessions decide again.
- The decisions come from the events `PartSuspended` and `PartResumed`. Never name an event `TenantSuspended`; that belongs to another product.

Register it before `AddArcAvailability` or replace the default:

```csharp
services.AddSingleton<ISuspensionSource, GuestListSuspensionSource>();
services.AddArcAvailability();
```

`InMemorySuspensionSource` is the default. It is what tests and single-process hosts use:

```csharp
await suspensions.Suspend(AudienceLevel.Tenant, "acme", "Fleet/DataQuality");
await suspensions.Resume(AudienceLevel.Tenant, "acme", "Fleet/DataQuality");
```

## When the source fails

A suspension source that cannot answer is a failure, not a denial. The request gets a retryable error (`IsRetryable`, HTTP `503`) like any other availability failure, and a part is never reported as suspended because the store is down.
