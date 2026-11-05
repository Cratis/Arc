---
title: Recheck Cratis Arc query access with emission guards
description: Use Cratis Arc emission guards to recheck access before delivering query results, suppress an update, or end a subscription when access changes.
---
<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

An emission guard in Cratis Arc rechecks access before a query update reaches the caller. Use one when a session can end or membership can change while an observable query stays open; subscription-time authorization alone does not check those later changes.

Implement `IGuardObservableQueryEmission` to check the current session or permission before delivery. Arc calls guards for observable emissions on the multiplexed hub, direct WebSocket and Server-Sent Events (SSE) connections, and HTTP snapshots. Guards also check non-streaming query results delivered through the hub. [Read-model interception](read-model-interception.md) runs before the guard, so the access decision applies before the intercepted result leaves the server.

A guard runs when there is a result to deliver, not on a timer. It can stop the next delivery after access changes; it does not by itself disconnect an idle subscription at the instant a session expires.

## Writing a guard

To stop delivery after sign-out, look up the session identified by the caller's `sid` claim. In this example, `ISessions` is an application-owned contract, not an Arc API. Register an implementation backed by your session store. This guard requires an active session for every stream or hub snapshot it evaluates, including queries that otherwise permit anonymous access.

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Queries;

namespace Application.Security;

public interface ISessions
{
    Task<bool> IsActive(string sessionId);
}

public class SessionMustStillBeActive(ISessions sessions) : IGuardObservableQueryEmission
{
    public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
    {
        var sessionId = context.Principal?.FindFirst("sid")?.Value;
        if (sessionId is null)
        {
            return ObservableQueryEmissionVerdict.DenyAndTerminate;
        }

        return await sessions.IsActive(sessionId)
            ? ObservableQueryEmissionVerdict.Allow
            : ObservableQueryEmissionVerdict.DenyAndTerminate;
    }
}
```

Arc discovers guard implementations by convention; you do not register the guard itself. Its dependencies, such as `ISessions`, must be available from dependency injection. Without a guard, Arc skips emission-guard dispatch. A filter-supplied subscription scope is still captured when the query starts.

## Compare the scope captured at subscription time

When a query filter chooses which organization's data to stream, its decision may change while the connection stays open. Put a serializable value in `QueryContext.SubscriptionScope` during filtering in Arc's query pipeline. Arc captures it **after the filters succeed and before the query executes**, then passes a fresh copy in `ObservableQueryEmissionContext.SubscriptionScope` on each emission. Values assigned during query execution are too late and do not change the captured scope. The guard can compare that original scope to current membership without rerunning the filter or changing the query arguments.

Controller-based observable queries do not run `IQueryFilters` through `QueryActionFilter.EstablishQueryContext`. Their emission guards always receive a `null` subscription scope; use a query handled by Arc's query pipeline if you need a filter-supplied scope.

The next example captures the organization chosen when the subscription starts, then compares it with current membership before delivery. `IMemberships` is an application-owned contract: its implementation must read current membership rather than a login-time cache. Replace the query name with your query's fully qualified name and use the same filter-derived organization in the query's data selection. Setting `SubscriptionScope` records the decision; it does not filter rows.

```csharp
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;

namespace Application.Security;

public interface IMemberships
{
    Task<string?> CurrentOrganizationId(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

public class CaptureOrganizationScope(ICurrentPrincipalAccessor principalAccessor, IMemberships memberships) : IQueryFilter
{
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        if (context.Name.Value != "Application.Queries.ForCurrentOrganization")
        {
            return QueryResult.Success(context.CorrelationId);
        }

        var principal = principalAccessor.Current;
        if (principal is null)
        {
            return QueryResult.Unauthorized(context.CorrelationId);
        }

        var organizationId = await memberships.CurrentOrganizationId(principal, context.CancellationToken);
        if (organizationId is null)
        {
            return QueryResult.Unauthorized(context.CorrelationId);
        }

        context.SubscriptionScope = organizationId;
        return QueryResult.Success(context.CorrelationId);
    }
}

public class OrganizationMustStillMatch(IMemberships memberships) : IGuardObservableQueryEmission
{
    public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
    {
        if (context.QueryName.Value != "Application.Queries.ForCurrentOrganization")
        {
            return ObservableQueryEmissionVerdict.Allow;
        }

        if (context.Principal is null || context.SubscriptionScope is not string originalOrganization)
        {
            return ObservableQueryEmissionVerdict.DenyAndTerminate;
        }

        var currentOrganization = await memberships.CurrentOrganizationId(context.Principal, context.CancellationToken);
        return currentOrganization == originalOrganization
            ? ObservableQueryEmissionVerdict.Allow
            : ObservableQueryEmissionVerdict.DenyAndTerminate;
    }
}
```

If a subscription captures `former` and membership later changes to `new`, the next emission still carries `former`. The guard denies that delivery even though the query arguments have not changed. A new subscription captures `new` independently.

Keep the scope small, using primitives or records of primitive values. Arc snapshots it with its configured `JsonSerializerOptions`; private fields, `[JsonIgnore]` members, and derived members of non-polymorphic base-typed properties do not survive serialization. Live services and mutable authorization caches therefore do not belong in the scope. An absent scope is `null`. A supplied scope that cannot be serialized and restored fails the query as `InvalidSubscriptionScope` rather than silently becoming `null`.

## The three verdicts

| Verdict | What happens |
| --- | --- |
| `Allow` | The intercepted emission is delivered. |
| `Suppress` | This one emission is withheld. The subscription stays live and the next emission is evaluated again. |
| `DenyAndTerminate` | Nothing is written, the client is told it is unauthorized, and this subscription is torn down. |

For an HTTP observable snapshot, `Allow` returns intercepted data with **200 OK** and `DenyAndTerminate` returns **403 Forbidden** without data. `Suppress` returns **202 Accepted** without data if you did not request waiting. With `waitForFirstResult=true`, Arc keeps evaluating emissions until one is allowed, the query is denied, or the original deadline expires with **408 Request Timeout**. Suppression does not restart the deadline; every candidate for this snapshot carries `IsFirstEmission=true`.

For a non-streaming query delivered through the hub, the guard receives `IsSnapshot=true` and `IsFirstEmission=true`. `Allow` sends the one-shot result; `DenyAndTerminate` sends `Unauthorized` without data (and SSE subscribe returns 401). `Suppress` sends no result, error, or denial frame and ends the operation: there is no stream to evaluate again. The SSE subscribe POST returns 200 for suppression, not an indication that data arrived.

`Suppress` does **not** move the delta baseline. The client never saw the withheld emission, so the next delivered `ChangeSet` is still computed against the last state it actually received — nothing goes missing.

`DenyAndTerminate` ends only the subscription it was given. Every sibling subscription on the same multiplexed connection keeps streaming. On the hub the client receives an `Unauthorized` message for that query id and the subscription is deleted; on a direct connection it receives a final `QueryResult` with `IsAuthorized` false and the stream closes. In both cases the Arc client latches the denial and stops reconnecting — a reconnect would only be denied again, and on a direct SSE connection the browser would otherwise re-establish the stream every few seconds and re-run the whole query for each attempt.

## What the guard is told

`ObservableQueryEmissionContext` carries the fully qualified query name, the coerced query arguments, the captured `SubscriptionScope` (or `null`), the caller's `ClaimsPrincipal`, the correlation id, whether this is the first emission on the subscription, `IsSnapshot` (true only for non-streaming hub results; false for stream emissions and HTTP observable snapshots), the subscription's cancellation token, and the `IServiceProvider` to resolve from. Each guard receives its own copy of the scope and arguments; mutation by one guard cannot change another guard's input or a later emission.

For the single-query WebSocket transport, `IsFirstEmission` stays true until a complete transport write is confirmed. A closed socket, cancellation before writing, or an unconfirmed write does not advance that hint or increment delivered-push telemetry. A completed write is not an acknowledgment that the client received or consumed the message.

Custom `IWebSocketConnectionHandler` implementations can also implement `IWebSocketMessageSender` to report explicit write outcomes. Only `WebSocketSendStatus.Sent` advances delivery state. The existing `SendMessage` contract remains available, but its null return cannot confirm a write: legacy-only handlers are treated as unconfirmed, so their first-emission hint stays true and they do not increment delivered-push telemetry. The hint is best-effort delivery state, not an authorization boundary.

Use the principal supplied in the context. A producer can emit outside the original request's execution context, so an ambient `AsyncLocal` value is not a reliable source for the subscription's identity.

## Several guards

The most restrictive verdict wins: `DenyAndTerminate` takes precedence over `Suppress`, which takes precedence over `Allow`. A denial stops evaluation immediately. Do not make one guard depend on another having run first; discovery order is not a public contract.

## Failing closed

A non-fatal exception while constructing or running a guard is logged at `Error` and treated as `DenyAndTerminate`. Arc withholds the result rather than treating an unavailable access check as permission to deliver it.

Because all discovered guards apply across queries, a guard that cannot be constructed can deny deliveries throughout the application. Check its real constructor and dependency registrations, not only its `Guard` method. The client sees an unauthorized result; use the server log to distinguish a failed check from an intentional denial.

Honor the context's `CancellationToken` in asynchronous lookups. An `OperationCanceledException` caused by cancellation of that token ends delivery without an error log: a client disconnect is ordinary teardown, not a failed authorization check.

## Cost

Delivery waits for the guard on every emission of every subscription. Choose a lookup that meets your revocation needs without unnecessary work: a local revocation list or short-lived cache may avoid a remote lookup, but stale entries delay an access change taking effect.

Arc resolves guards with `GetServiceOrCreateInstance`. Unless you explicitly register the concrete type, a guard such as `SessionMustStillBeActive` is constructed for each emission and is not tracked for disposal. Its name does not match Arc's `IFoo → Foo` registration convention. Keep the constructor trivial and put shared state, such as a cache, in an injected service rather than rebuilding it in the guard.

Resolve scoped collaborators through the constructor. They come from the subscription's service scope and are disposed with it, so the guard can use a scoped session store or tenant-aware service.

## Where it does not help

Guards protect delivery, not data selection. Keep subscription-time authorization and caller-scoped storage queries in place. Direct HTTP non-observable queries do not invoke emission guards; each request goes through its normal authentication and query authorization. Review [read-model interception's opt-out boundaries](read-model-interception.md) when a response also depends on a transformation.

On a WebSocket the principal is frozen at the handshake — the protocol offers no way to present fresh credentials on an established connection. The identity a guard receives on a WebSocket subscription is the one captured when the socket was upgraded, and it does not change for the life of that connection.

Use that captured identity to look up current session or permission state, rather than expecting its claims to change in place. After implementing the guard, verify denial both before the first result and after earlier results have been delivered, through each transport you expose.

## See also

- [Observable Query Hub](./observable-query-demultiplexer.md) — the multiplexed transport and its subscription lifecycle.
- [Read Model Interception](./read-model-interception.md) — transforming each emitted read model before it is written.
- [Authorization](../core/authorization.md) — the subscription-time verdict this builds on.
