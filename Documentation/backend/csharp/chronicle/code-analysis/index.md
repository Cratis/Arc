---
title: Code analysis
description: Build-time diagnostics for Arc's optional Chronicle integration, including their heuristics and boundaries.
---

These `ARCCHR####` Roslyn diagnostics belong to **Arc's Chronicle integration**, not to standalone Arc or every project that references pure Chronicle. They catch integration mistakes while you build, so a misplaced key, ambiguous identity, or unsupported handler shape is visible before a request reaches production.

## Rules overview

| Rule | Severity | What it checks |
| --- | --- | --- |
| [ARCCHR0001](./ARCCHR0001.md) | Error | Recognized aggregate event-handler candidates have supported signatures. |
| [ARCCHR0002](#arcchr0002-ambiguous-command-identity) | Warning | Multiple candidate command identities without an explicit provider/recognized return exemption. |
| [ARCCHR0003](./ARCCHR0003.md) | Warning | Reactor access to the default event log instead of returned side effects. |
| [ARCCHR0004](./ARCCHR0004.md) | Warning | Redundant explicit id on `[EventType]`: empty, or equal to the type name. |
| [ARCCHR0005](./ARCCHR0005.md) | Warning | Chronicle usage and `AddCratisArc` appear in one project without integration setup. |
| [ARCCHR0006](./ARCCHR0006.md) | Warning | Manual reactor command execution without method- or class-level `[OnceOnly]`. |
| [ARCCHR0007](#arcchr0007-command-handler-injects-ieventlog) | Warning | A command `Handle()` or `Provide()` parameter is `IEventLog` or an implementing type. |
| [ARCCHR0008](./ARCCHR0008.md) | Warning | Data annotations `[Key]` used where Chronicle key resolution applies. |
| [ARCCHR0009](./ARCCHR0009.md) | Warning | Likely secret command values lack audit exclusion metadata. |
| [ARCCHR0010](./ARCCHR0010.md) | Warning | A keyless command returns a raw Guid beside statically identifiable untargeted events. |

## ARCCHR0002: ambiguous command identity

Use one identity candidate or implement `ICanProvideEventSourceId`. Positional Chronicle `[Key]` and the matching property count as one candidate. Recognized explicit identity/wrapper return signatures can exempt the command.

This analyzer's candidate convention includes implicit conversions; runtime discovery does not generally do so. A conversion-only `ConceptAs<Guid>` is not a safe runtime identity declaration. Use `EventSourceId<Guid>` ancestry or a selected key. Neither a clean analyzer result nor a return exemption proves that input-time aggregate/read-model dependencies use your intended id. See [identity timing](../resolving-event-source-id.md).

## ARCCHR0007: command handler injects IEventLog

Prefer [returned events](../commands/events.md). This rule matches `IEventLog` (and implementing-type) parameters on `Handle()` and `Provide()`, including read-only use; it does not inspect whether they append. Deliberate exact-revision reads or advanced explicit transactional appends may warrant a narrow suppression. The API remains supported at runtime, with the boundaries in [Transactional commands](../commands/transactional-commands.md). A warning is not a runtime prohibition.

## Quick fixes

| Rule | Fix |
| --- | --- |
| [ARCCHR0008](./ARCCHR0008.md) | Rewrite to the fully qualified Chronicle Key attribute. |
| [ARCCHR0010](./ARCCHR0010.md#quick-fix) | Compiler-checked local replacement with `EventSourceId<Guid>` for supported direct tuple syntax. |

The other rules have no automatic fix. A fix can change behavior: in particular ARCCHR0010 changes the persistence target when the Guid was an ordinary response.

## Installation

Install the optional Arc integration package:

```bash
dotnet add package Cratis.Arc.Chronicle
```

It brings in `Cratis.Arc.Chronicle.CodeAnalysis`, which supplies these analyzers and code fixes. Complete the [integration setup](../cratis-package.md) to connect Arc's command pipeline to Chronicle.

Pure `Cratis.Chronicle` provides event sourcing without Arc; referencing it alone does not install Arc's integration diagnostics. If a diagnostic is missing, check that your application's resolved dependencies include `Cratis.Arc.Chronicle.CodeAnalysis`.
