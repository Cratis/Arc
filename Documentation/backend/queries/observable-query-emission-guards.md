# Observable Query Emission Guards

Authorization for an observable query runs when the subscription is established. That verdict decides whether the caller may **obtain** the live stream — it says nothing about the minutes or hours the stream then stays open.

That is fine for most applications. Subscriptions end when the user navigates away, the tab closes, or the server shuts down. But a subscription has no other reason to end: nothing terminates it when a token expires, a session is signed out, or a role is revoked. Keep-alive pings deliberately stop proxies from culling an idle connection, and the client reconnects and re-subscribes on its own.

An **emission guard** closes that gap. Implement `IGuardObservableQueryEmission` and Arc consults it for every emission an observable query is about to write, on every transport — the multiplexed hub and the direct WebSocket/SSE connections alike.

## Writing a guard

```csharp
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

That is the whole opt-in. Guards are discovered by convention — no registration, no configuration. An application with no guard pays nothing: no context is built and nothing is dispatched, and emissions take exactly the path they took before.

## The three verdicts

| Verdict | What happens |
| --- | --- |
| `Allow` | The emission is written unchanged. |
| `Suppress` | This one emission is withheld. The subscription stays live and the next emission is evaluated again. |
| `DenyAndTerminate` | Nothing is written, the client is told it is unauthorized, and this subscription is torn down. |

`Suppress` does **not** move the delta baseline. The client never saw the withheld emission, so the next delivered `ChangeSet` is still computed against the last state it actually received — nothing goes missing.

`DenyAndTerminate` ends only the subscription it was given. Every sibling subscription on the same multiplexed connection keeps streaming. On the hub the client receives an `unauthorized` message for that query id; on a direct connection it receives a final `QueryResult` with `IsAuthorized` false and the stream closes. The client does not reconnect after that — a reconnect would only be denied again.

## What the guard is told

`ObservableQueryEmissionContext` carries the fully qualified query name, the coerced query arguments, the caller's `ClaimsPrincipal`, the correlation id, whether this is the first emission on the subscription, the subscription's cancellation token, and the `IServiceProvider` to resolve from.

The principal is handed over **explicitly**. Emissions arrive on the producing stream's own thread, where the request's `AsyncLocal` context does not flow, so a guard that reached for an ambient accessor would see the wrong identity — or none at all.

## Several guards

Every guard is asked, and the most restrictive verdict wins: `DenyAndTerminate` over `Suppress` over `Allow`. The first `DenyAndTerminate` short-circuits the rest.

## Failing closed

A guard that throws is treated as `DenyAndTerminate`. The failure is logged and never rethrown.

This is deliberate. A guard that failed open would leave an application believing its stream is protected while it keeps flowing — which is worse than having no guard at all, because nobody goes looking.

## Cost

The guard runs on **every** emission of **every** subscription. Keep it fast: prefer cached state, a local revocation list, or a short-TTL lookup over a network round trip per emission.

Resolve collaborators through the constructor. They come from the subscription's own scope and are disposed with it, so a guard can safely hold a scoped session store or tenant-aware service.

## Where it does not help

On a WebSocket the principal is frozen at the handshake — the protocol offers no way to present fresh credentials on an established connection. The identity a guard receives on a WebSocket subscription is the one captured when the socket was upgraded, and it does not change for the life of that connection.

That does not make the guard useless there; it makes it a lookup rather than a re-read. Take the identity from the context and ask your own source of truth — a session store, a token introspection endpoint, a revocation list — whether that identity is still good.

## See also

- [Observable Query Hub](./observable-query-demultiplexer.md) — the multiplexed transport and its subscription lifecycle.
- [Read Model Interception](./read-model-interception.md) — transforming each emitted read model before it is written.
- [Authorization](../core/authorization.md) — the subscription-time verdict this builds on.
