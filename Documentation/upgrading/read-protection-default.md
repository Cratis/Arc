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
- Read-model parameters that cannot be guarded, and missing generated key metadata or key accessors, are refused at startup or at first use. Mark a deliberately advisory
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

## Update command scenario state setup

With read protection `On` by default since v23.0.0, replacing or substituting `IReadModels` no longer supplies
state to protected decision reads in a `CommandScenario`. This includes readers from a separate
`ReadModelScenario` and mocks. A model supplied only through that replacement can remain `null` in the command.

Arrange state through the command scenario's own `Given` instead. For a plain read-model parameter, pin the
instance with `_scenario.Given.ForEventSource(id).ReadModel(instance)`, or seed its history with
`_scenario.Given.ForEventSource(id).Events(...)`. Chronicle admits and enrolls the protected read first, then
uses the scenario's pin or the state derived from its seeded events; it does not use the replacement reader's
lookup result. Pins supply state without replacing admission or conflict detection.

Follow the [before/after command scenario setup](../backend/csharp/testing/chronicle.md#testing-commands-that-take-read-model-dependencies)
and the [protected read-model setup](../backend/csharp/testing/chronicle.md#give-a-protected-read-model-its-state)
for a complete spec and the cases that require events rather than pins.

## Check the result

Build with the analyzers enabled and run the specs of every command that injects a read model. A command that
needed a lookup key or an opt-out fails the build or the spec instead of changing behavior silently in production.

## Strict key coverage is the default

Two related defaults change in the same release:

- `ReadProtectionConfiguration` now requires generated command read-model key metadata unless `ReadProtection:RequireGeneratedMetadata` is set to `false`. Commands without generated metadata fail at startup with `CommandReadModelKeysMissing` instead of reflecting. Enable the Arc source generator, or opt out with `false` on runtimes with dynamic code support.
- `ProtectedReadModelKeyOptions.Strict` defaults to `true`, so an uncovered composite key part throws `ProtectedReadModelKeyAccessorMissing`. Register the key accessors, or set `Strict` to `false` to keep the reflection fallback.
