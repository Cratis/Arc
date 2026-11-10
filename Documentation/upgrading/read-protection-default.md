---
title: Read protection is on by default
description: Arc 23 turns read protection On for commands and projects that declare no mode. Keep the Arc 22 behavior with one property, or adopt On through Measure.
---

## What changed

In Arc 22, a project that did not set `<CratisReadProtection>` ran with read protection `Off`: an injected read
model was a plain snapshot read that guarded nothing. From Arc 23 the default is `On`. Arc reads supported models
fresh, guards the append against what the command read, and refuses read-model parameters it cannot guard.

A command or project that declares a mode keeps it. Only commands with no declaration anywhere change.

## Who is affected

Applications that use Arc with Chronicle and inject read models into commands, validators, `Provide()` or
`Handle()`, and that never set `<CratisReadProtection>` or `[ReadProtection]`.

What you may see after upgrading:

- Appends that raced with a concurrent change are refused where they previously succeeded.
- Read-model parameters that cannot be guarded are refused at startup or at first use. Mark a deliberately advisory
  read with `[Unprotected("why")]`, or select the instance with `[LookUpBy(nameof(Property))]`.
- Each guarded read is a fresh fold of the key's events instead of the materialized state, so reads cost more
  for keys with long histories.

## Keep the Arc 22 behavior

Set the mode explicitly in each project:

```xml
<PropertyGroup>
    <CratisReadProtection>Off</CratisReadProtection>
</PropertyGroup>
```

Or opt out for one command with `[ReadProtection(ReadProtectionMode.Off)]` or `[Unprotected("why")]`, or for one
read with `[Unprotected("why")]` on the parameter, the read-model type or the method.

## Adopt On gradually

1. Set `<CratisReadProtection>Measure</CratisReadProtection>` and run your workload or specs.
2. Fix what the measurement reports. See [Adopting read protection](/arc/backend/csharp/chronicle/consistency/adopting/).
3. Remove the property, or set it to `On`, to take the new default.

## Check the result

Build with the analyzers enabled and run the specs of every command that injects a read model. A command that
needed a lookup key or an opt-out fails the build or the spec instead of changing behavior silently in production.
