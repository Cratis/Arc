---
title: Queries
description: Find the core query contracts and React guides for ordinary and observable queries.
---

Arc queries retrieve typed data from controller-based or model-bound endpoints. Ordinary queries use HTTP request/response; observable queries subscribe to an emitting backend source. Neither requires Chronicle.

This compatibility page retains the original route. The maintained reference now lives in the [core query section](./queries/index.md).

## IQuery interface

See [query contracts](./queries/contracts.md) for sorting, paging, parameters, and result shapes. Use generated models and proxies rather than copying incomplete interface declarations from examples.

## IQueryFor interface

Ordinary query instances expose `perform(args?)`. Observable instances expose subscriptions. React hooks wrap their results in state and return tuples; see [React query usage](../react/queries/usage.md).

## Validation

Projected client rules can reject requests before transport; the server remains authoritative. See [validation and behavior](./queries/validation-and-behavior.md).

## Configuration

Use [query configuration](./queries/configuration.md) for origins, service routing, API base paths, HTTP query methods, and cache retention.

### Observable query transport (queryDirectMode)

`queryDirectMode` chooses direct per-query URLs versus shared hubs. `queryTransportMethod` independently chooses SSE versus WebSocket. All four combinations exist. SSE hubs share EventSource connections and use subscribe/unsubscribe POSTs; they do not open one stream per query. See the [transport matrix](../react/queries/observable-query-multiplexing.md).

## Error handling

Inspect readiness before treating an unsuccessful result as a failure, then inspect authorization, validation, and exceptions. Defaults are not evidence of a successful load. [Suspense boundaries](../react/queries/suspense-queries.md) handle selected initial failures, not every invalid result or an automatic retry policy.

## Next steps

- [Core query reference](./queries/index.md)
- [React queries](../react/queries/index.md)
- [Commands](./commands/index.md)
- [MVVM](../react.mvvm/index.md)
- [Proxy generation](../../backend/proxy-generation/index.md)
