---
title: Read-model interception
description: Transform read-model instances and understand where interception is not applied.
---
<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

An interceptor lets you apply a cross-cutting transformation without repeating it in each query. Typical uses include localization, enrichment, and controlled field transformations. It is not a replacement for query authorization or safe data selection.

## Supported paths and exceptions

| Path | Interception |
| --- | --- |
| Ordinary model-bound query data | Applied after rendering |
| Ordinary Arc-wrapped MVC GET data | Applied after rendering |
| Supported direct WebSocket/SSE streams and multiplexed hub emissions | Applied per emission |
| Observable HTTP snapshot, including `waitForFirstResult=true` | Applied before the first allowed result is returned |
| MVC `[AspNetResult]` | Opts out of the Arc result-processing path |

> [!WARNING]
> Interception transforms data; it does not authorize access. Ensure the query/provider selects only rows and fields the caller may receive on **every** exposed path. MVC `[AspNetResult]` still opts out of Arc result processing.

```mermaid
flowchart LR
    Query[Query result] --> Ordinary[Ordinary data]
    Ordinary --> Interceptor[Interceptors]
    Query --> Stream[Stream wrapper]
    Stream --> Emission[Streaming emission]
    Emission --> Interceptor
    Stream --> Snapshot[HTTP snapshot]
    Snapshot --> Interceptor
    Interceptor --> Client[Client]
```

## Implement an interceptor

This type example reuses the shared [`AccountId` concept](model-bound/index.md#model-account-identities-and-names); the host discovers `IInterceptReadModel<T>` implementations and resolves constructor dependencies from the supplied service provider. This example enriches public display data, rather than using masking as access control.

```csharp
using System.Globalization;
using System.Threading.Tasks;
using Cratis.Arc.Queries;

namespace Banking.Accounts;

public record AccountSummary(AccountId Id, decimal Balance, string FormattedBalance);

public class FormatAccountBalance : IInterceptReadModel<AccountSummary>
{
    public Task<AccountSummary> Intercept(AccountSummary readModel) =>
        Task.FromResult(readModel with
        {
            FormattedBalance = readModel.Balance.ToString("C", CultureInfo.GetCultureInfo("en-US"))
        });
}
```

The returned instance is served to the caller. Prefer a `with` copy over mutating a record that another subscriber may share. An interceptor is bound to its exact model type; a DTO with the same fields is not automatically intercepted.

## Multiple interceptors and streams

Interceptors run in discovery order. Avoid making security depend on an undocumented ordering between implementations. Collections are processed item by item; streaming paths process each emitted model before delivery.

The pipeline skips stream wrappers because a wrapper is not a model instance. Streaming transports and observable HTTP snapshots intercept the emitted values instead, using the requesting subscriber's identity and scoped services. Interception runs before [emission guards](observable-query-emission-guards.md); a failing interceptor returns an error, never the raw value. Test ordinary GET, observable snapshot GET, direct SSE/WebSocket, and hub delivery separately for any transformation the application depends on.

Continue with [observable queries](model-bound/observable-queries.md) for lifetime rules and [emission guards](observable-query-emission-guards.md) for per-emission access decisions.
