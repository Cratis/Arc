---
title: Cratis Arc availability gates for commands and queries
description: Refuse commands and queries of a part of your Cratis Arc application that is not available to a tenant, user or system caller, tell clients why, and keep MCP tools and clients in step.
---

Authorization says who may do something. Availability says whether a part of the application is there at all for a caller: a commercial package, a preview, a capability switched off for an incident. Arc enforces availability in the command and query pipelines, before authorization, and tells clients with a reason that is different from an authorization denial.

Arc hosts the hook only. The decision comes from the [Choreography availability engine](https://github.com/Cratis/Choreography), which resolves audiences and context values and evaluates gate expressions. Hosts that do not add `Cratis.Arc.Availability` do not load the engine and pay nothing: no gate is consulted and every part is available.

## Add availability to a host

Add the `Cratis.Arc.Availability` package, register the Choreography pieces you own, and call `AddArcAvailability`:

```csharp
services.AddSingleton<IAudienceSource>(new InMemoryAudienceSource());
services.AddSingleton<IDeclaredAudiences>(new DeclaredAudiences("Plus", "Preview"));
services.AddSingleton(new AvailabilityCatalog());
services.AddSingleton(new ContextValueCatalog());
services.AddArcAvailability(options => options.GateNamespace("Fleet.DataQuality", AvailabilityExpression.For("Plus")));
```

Replace `InMemoryAudienceSource` with an `IAudienceSource` over your own storage in a real product. `AddArcAvailability` also registers the engine through Choreography's `AddAvailability`, and accepts a second callback to configure its options.

## Gate a part

A part is a command, or the read model that declares queries. Gate it with attributes, with options, or both. All gates that apply must hold: the gates of enclosing namespaces (outermost first), then the gate registered for the part, then its attributes.

```csharp
[Command]
[AvailableFor("Plus", "Preview")]
public record ResetScore(VesselId Vessel)
{
    public ScoreReset Handle() => new(Vessel);
}

[ReadModel]
[AvailableWith("DataQuality")]
public record QualityScores(VesselId Vessel, int Score)
{
    public static IEnumerable<QualityScores> All(IMongoCollection<QualityScores> scores) =>
        scores.Find(_ => true).ToList();
}
```

`[AvailableFor]` lists audiences, any one of which opens the gate. `[AvailableWith]` names a reusable `AvailabilityDefinition` in the host's `AvailabilityCatalog`. Several attributes on one type must all hold. For anything else, register a gate with `options.Gate<TPart>(expression)` or `options.GateNamespace(namespace, expression)` using Choreography's `AvailabilityExpression`.

An audience, a context value and the rules of the three-valued result are described in the Choreography documentation. In short: an unknown decision, such as a missing audience or an absent value, never opens a gate.

## What a refused request looks like

A command or query in a part that is not available is refused before `authorize` is evaluated, so nothing else about the request, such as its validation, is reported to the caller.

| Decision | Result |
| --- | --- |
| Available | The request continues to authorization and validation. |
| Unavailable or unknown | `IsAuthorized` is `false`, `AuthorizationFailureReason` is `NotAvailable` (`AvailabilityReason.NotAvailable`), HTTP status `403`. |
| The decision could not be made | A retryable error: `IsRetryable` is `true`, `IsAuthorized` stays `true`, HTTP status `503`. |

An unavailable and an unknown decision are reported identically, so a client is told no more than that the part is not available. Which gate part closed the decision is logged on the server and never sent.

A failure, such as a store that is down or a context value provider that throws, is never reported as a denial. The caller gets a retryable error, and the same request can succeed when retried. Cancellation is not a failure: a cancelled request is cancelled.

`QueryResult` carries `AuthorizationFailureReason` and `IsRetryable` like `CommandResult` does. Observable queries are refused the same way when the subscription starts.

## Read availability on the client

The `/.cratis/availability` observable query reports what is not available to the current context. It emits the current decision first and again each time it changes, so open sessions update without a reload. It re-emits only when the answer is different.

```json
{
  "unavailable": [
    { "name": "Fleet.DataQuality.QualityScores", "module": "Fleet", "feature": "DataQuality", "slice": "QualityScores" }
  ]
}
```

Everything not listed is available. A part is named by the full name of its command or read model type and carries the slice it belongs to, which is the same slice location the generated proxies carry. A decision that cannot be made is a retryable error for the query, never a list: clients keep what they last knew.

Audience changes reach open sessions because the gate subscribes to the `IAudienceSource`. Each change makes the session decide again from current state, not from what was resolved when it started.

[React clients](../../frontend/react/availability.md) hide navigation, screens and parts from this query.

## MCP tools

An MCP tool whose command or query is not available to the caller is omitted from the tool listing. A call to it is refused with the same `NotAvailable` reason in the tool outcome (`reason`), and a failure to decide is reported with `isRetryable`. If deciding fails while listing, the tool stays listed, because omitting it would report the failure as "not available"; the call itself is refused as a retryable error.

## Scope and limits

- Availability applies to model-bound commands and queries, including observable queries, on the Arc pipeline and MCP. Controller actions use ASP.NET Core authorization and are not gated.
- Events, projections, observers and reactors are not touched. A reaction can produce into a part that is not available, and projections keep running, so switching a part on again needs no replay.
- Clients decide per slice. A part gated alone inside a slice with other parts hides the whole slice from slice-based client checks, which is the safe direction; the server still enforces each part exactly.
- A request lives in one context. The audiences and context values of a request are resolved once and shared by every gate in it, and by [the values your handlers read](context-values.md).
- Availability is separate from Box Office module entitlements, which are unchanged.
