---
title: Availability engine
description: Decide per request whether part of your application exists for a tenant, user or system caller, using audiences, gate expressions and lazy context values.
---

Authorization says who may do something. Availability says whether a part of the application is there at all for a given
audience: a commercial package, a preview, a switched-off capability. The availability engine in
`Cratis.Arc.Availability` evaluates that decision for one request. The engine is plain .NET with no dependency on ASP.NET,
so a host that is not a web application can use it. [Availability gates](../availability.md) describe how Arc applies it to commands and queries.

The engine has three pieces:

| Piece | What it does | Page |
| --- | --- | --- |
| Gate evaluation | Evaluates `for`, `when` and `with` parts combined with `and`, `or` and parentheses. | [Evaluating gates](evaluating-gates.md) |
| Audiences | Resolves which audiences apply to a request, from the tenant, the user and the system caller. | [Audiences](audiences.md) |
| Context values | Resolves the values a gate reads, lazily and once per request. | [Request-scoped context values](context-values.md) |
| Manifest | Describes what the model declares as data, and expands gates into alternatives. | [The availability manifest](manifest.md) |

Three rules hold everywhere:

- **Unknown denies.** A value that is absent, or audiences that were not resolved, make a gate unknown, and an unknown
  gate is not open.
- **Absence is not failure.** A missing key or an empty query result is unknown. A store that is down or code that
  throws is an exception, so an outage is never shown as "not available".
- **Availability has its own reason.** A closed gate reports `AvailabilityReasons.NotAvailable`, which clients can tell
  apart from an authorization denial.

## Set it up

```csharp
using Cratis.Arc.Availability;
using Cratis.Arc.Availability.Audiences;

services.AddSingleton<IDeclaredAudiences>(new DeclaredAudiences("Plus", "Preview")); // Without this, the audience types of the application are found with ITypes.
services.AddAvailability(options => options.EnvironmentName = environment.EnvironmentName);
```

`AddAvailability` registers `IAudienceResolver`, `IAvailabilityEvaluator`, the `AvailabilityCatalog` and the
`ContextValueCatalog`. It also defaults the audience source to `InMemoryAudienceSource` and declares no audiences. A
registration you make before the call is kept, so register an implementation of `IAudienceSource` over your own storage for
a real product. In an Arc application, [`WithAvailability`](../availability.md) does this and the gate wiring in one call.
