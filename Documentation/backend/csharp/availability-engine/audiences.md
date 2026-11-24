---
title: Audiences
description: Resolve the audiences that apply to a request from tenant, user and system grants, react to changes, and grant every audience in local and test environments.
---

An audience is a named group a tenant, a user or the system caller can belong to. The model declares which audiences
exist. Who holds them is runtime data and changes without a redeploy.

## Where grants come from

`IAudienceSource` is the one abstraction you implement over your storage:

| Member | Purpose |
| --- | --- |
| `GetGrants(subject, cancellationToken)` | The `AudienceGrant`s that apply to an `AudienceSubject` (tenant, user, system), each tagged with its `AudienceLevel`. |
| `OnChanged(handler)` | Subscribes to `AudienceChange` notifications, so a host can invalidate what it cached. Dispose the result to unsubscribe. |

An `AudienceSubject` can also carry the calling system's client id in `SystemClientId`, set with an object initializer
(`new AudienceSubject(tenant, user, IsSystem: true) { SystemClientId = "billing" }`). It is empty when the caller names
none. The resolver hands the subject to the source unchanged, so a source can return the system-wide grants plus that
system's own; a caller with no client id gets the system-wide grants only.

`InMemoryAudienceSource` is a complete implementation for specifications and samples. Its `Grant` and `Withdraw` methods
take effect immediately and notify subscribers. Withdrawing at one level leaves the other levels alone.

## Effective audiences

`IAudienceResolver.Resolve(subject)` returns `EffectiveAudiences`: the union of the tenant, user and system grants. There is
no precedence between levels.

| Member | Meaning |
| --- | --- |
| `Matching` | The declared audiences held. Only these match a gate. |
| `Undeclared` | Grants whose audience the model does not declare. |
| `Holds(audience)` | Whether a declared audience is held. |

A grant for an undeclared audience is carried and reported, never dropped and never matched. Register an
`IUndeclaredAudienceObserver` to be told about them, for example to show them to an operator. Declaring the audience later
makes the old grant live again.

## The environment switch

For local development and test runs you can grant every declared audience to every request, so you see the whole
application:

```csharp
services.AddAvailability(options =>
{
    options.EnvironmentName = environment.EnvironmentName;
    options.GrantAllDeclaredAudiences = true;
});
```

The switch is off by default. It is allowed only in the `Development`, `Local`, `Test` and `Testing` environments.
In any other environment, including `Production`, an environment with no name, or a name that is not recognized, setting it
throws `EnvironmentSwitchRefusedException`: from `AddAvailability` and `AudienceResolver` at startup, and from `Resolve`
if it is turned on afterwards. Grants for undeclared audiences still do not match while the switch is on.
