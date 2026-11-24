---
title: Cratis Arc availability gates for commands and queries
description: Refuse commands and queries for parts of your Cratis Arc application that are unavailable to a tenant, user or system, tell clients why, and sync MCP tools.
---

Authorization says who may do something. Availability says whether a part of the application is there at all for a caller: a commercial package, a preview, a capability switched off for an incident. Arc enforces availability in the command and query pipelines, before authorization, and tells clients with a reason that is different from an authorization denial.

The decision comes from the [availability engine](availability-engine/index.md), which resolves audiences and context values and evaluates gate expressions. It ships in the same `Cratis.Arc.Availability` package and does not depend on ASP.NET. Hosts that do not call `WithAvailability` pay nothing: no gate is consulted and every part is available.

## Add availability to a host

Add the `Cratis.Arc.Availability` package and call `WithAvailability`:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc
    .WithAvailability());
```

That one call registers the gates, the engine, an in-memory audience source and the startup validation. It accepts a second callback to configure the engine's `AvailabilityOptions`, such as the environment name. Register an `IAudienceSource` over your own storage in a real product. The audiences, availability blocks, scope markers and provided read models are types that are discovered from the application, so there is nothing else to register. At startup, one pass discovers the gated parts and verifies that every block built on another is defined and that no blocks build on each other in a cycle.

A host that has no Arc builder can call `services.AddArcAvailability(...)`, which does the same. The options you configure are the ones that are resolved, whether `AddCratis()` ran before or after.

## Gate a part

A part is a command, or the read model that declares queries. Gate it with attributes, with options, or both. All gates that apply must hold: the gates of enclosing namespaces (outermost first), then the gate registered for the part, then its attributes.

```csharp
[Command]
[AvailableFor<Plus, Preview>]
public record ResetScore(VesselId Vessel)
{
    public ScoreReset Handle() => new(Vessel);
}

[ReadModel]
[Available<DataQuality>]
public record QualityScores(VesselId Vessel, int Score)
{
    public static IEnumerable<QualityScores> All(IMongoCollection<QualityScores> scores) =>
        scores.Find(_ => true).ToList();
}
```

The attributes refer to types; they do not describe rules. `[AvailableFor<Plus, Preview>]` names audiences, any one of which opens the gate. `[Available<DataQuality>]` names an availability block, and `[AvailableWhen<HasPaidInvoice>]` names a condition class. Several attributes on one type are several lines, and all of them must hold. Everything else, the rules themselves, is in the types they refer to: see [declaring availability with types](availability-engine/typed-declarations.md).

A scope marker gates a whole module, feature or slice: put `[AvailableFor<Plus>] public class Feature : AvailabilityScope;` in its namespace. For a single part, `options.Gate<TPart>(expression)` still registers a gate in code, but the model cannot read it back, so prefer the attributes.

[Gate evaluation](availability-engine/evaluating-gates.md), [audiences](availability-engine/audiences.md) and [context values](availability-engine/context-values.md) are described with the engine. In short: an unknown decision, such as a missing audience or an absent value, never opens a gate.

## System callers

The subject Arc builds for each request carries the tenant and the user. When the caller is the system, it also carries the client id of the calling system in `AudienceSubject.SystemClientId`, read from the `client_id` claim of the authenticated principal, or from `azp` when `client_id` is absent. Your `IAudienceSource` can then return the system-wide grants plus the grants of that client. A system caller whose principal carries neither claim passes an empty client id and gets the system-wide grants only. Person callers never pass a client id, even when their token carries one.

## What a refused request looks like

A command or query in a part that is not available is refused before `authorize` is evaluated, so nothing else about the request, such as its validation, is reported to the caller.

| Decision | Result |
| --- | --- |
| Available | The request continues to authorization and validation. |
| Unavailable or unknown | `IsAuthorized` is `false`, `AuthorizationFailureReason` is `NotAvailable` (`AvailabilityReason.NotAvailable`), HTTP status `403`. |
| Suspended | `IsAuthorized` is `false`, `AuthorizationFailureReason` is `Suspended` (`AvailabilityReason.Suspended`), HTTP status `403`. See [suspending parts](availability-engine/suspending-parts.md). |
| The decision could not be made | A retryable error: `IsRetryable` is `true`, `IsAuthorized` stays `true`, HTTP status `503`. |

An unavailable and an unknown decision are reported identically, so a client is told no more than that the part is not available. Which gate part closed the decision is logged on the server and never sent.

A failure, such as a store that is down or a context value provider that throws, is never reported as a denial. The caller gets a retryable error, and the same request can succeed when retried. Cancellation is not a failure: a cancelled request is cancelled.

`QueryResult` carries `AuthorizationFailureReason` and `IsRetryable` like `CommandResult` does. Observable queries are refused the same way when the subscription starts.

## Bound invocation context

`IAvailabilityGate` remains the type-only contract used by metadata and listing. A gate can also implement
`IAvailabilityExecutionGate`. The command and query filters prefer this optional capability and pass an
`AvailabilityInvocation`: the existing operation type, the bound command or coerced query arguments, and the
server's pinned `ReceivedAt` receipt. Gates implementing only `IAvailabilityGate` continue to work unchanged.
The query operation identity remains the performer's `Type`, not its `ReadModelType`.

Providers can resolve `arguments.<property>` and `receivedAt` through their resolver. Command
properties and query argument names are case-insensitive. The receipt is a `DateTimeOffset`, fixed at the Arc
entry boundary rather than read from the clock during evaluation. Manually constructed contexts keep their
existing default receipt; the gate does not invent one. An explicit invocation with null arguments has no
`arguments` value. Type-only calls have neither `arguments` nor `receivedAt`, including when a listing is
requested during an execution. Conditions needing an absent argument stay unknown and never permit execution.

Arguments remain untrusted input under the separate `arguments` root. They cannot replace the server's
`tenant`, `identity`, audience grants, declared context values or receipt. No service provider or principal is
exposed through the ambient bag. Host providers still use their own scoped collaborators to fetch domain data.

Resolved values are isolated by invocation identity and DI scope, not type, argument equality or receipt time.
The gate, handler and context-value readers of one invocation share its resolver, including after dependency
binding. Metadata uses a separate cache. Nested and parallel operations do not inherit another operation's
resolved values, even if their receipt times are identical. A provider failure or cancellation is propagated
and the affected resolver is cleared so a later resolution can retry; neither becomes an availability grant.
Do not reuse an `AvailabilityInvocation` instance for a different dispatch.

Controller context-value readers also receive the MVC-bound command body or query arguments and transport
receipt. This does not add availability filtering to controller actions or change authorization.

## Read availability on the client

The `/.cratis/availability` observable query reports what is not available to the current context. It emits the current decision first and again each time it changes, so open sessions update without a reload. It re-emits only when the answer is different.

```json
{
  "unavailable": [
    { "name": "Fleet.DataQuality.QualityScores", "module": "Fleet", "feature": "DataQuality", "slice": "QualityScores", "reason": "NotAvailable" }
  ]
}
```

The `reason` is `NotAvailable`, or `Suspended` for a part an operator suspended. Everything not listed is available. A part is named by the full name of its command or read model type and carries the slice it belongs to, which is the same slice location the generated proxies carry. A decision that cannot be made is a retryable error for the query, never a list: clients keep what they last knew.

Audience changes reach open sessions because the gate subscribes to the `IAudienceSource`. Each change makes the session decide again from current state, not from what was resolved when it started.

[React clients](../../frontend/react/availability.md) hide navigation, screens and parts from this query.

## MCP tools

An MCP tool whose command or query is not available to the caller is omitted from the tool listing. A call to it is refused with the same `NotAvailable` reason in the tool outcome (`reason`), and a failure to decide is reported with `isRetryable`. If deciding fails while listing, the tool stays listed, because omitting it would report the failure as "not available"; the call itself is refused as a retryable error.

## Scope and limits

- Availability applies to model-bound commands and queries, including observable queries, on the Arc pipeline and MCP. Controller actions use ASP.NET Core authorization and are not gated.
- Events, projections, observers and reactors are not touched. A reaction can produce into a part that is not available, and projections keep running, so switching a part on again needs no replay.
- Clients decide per slice. A part gated alone inside a slice with other parts hides the whole slice from slice-based client checks, which is the safe direction; the server still enforces each part exactly.
- A bound invocation lives in one context. Its successfully resolved audiences and context values are shared by its gates and [the values its handlers read](context-values.md). Another invocation, even in the same service scope, resolves independently.
- Availability is separate from Box Office module entitlements, which are unchanged.
