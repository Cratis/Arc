---
title: Code analysis
description: Build-time diagnostics for Arc's optional Chronicle integration, including their heuristics and boundaries.
---

These `ARCCHR####` Roslyn diagnostics belong to **Arc's Chronicle integration**, not to standalone Arc or every project that references pure Chronicle. They catch integration mistakes while you build, so a misplaced key, ambiguous identity, or unsupported handler shape is visible before a request reaches production.

## Rules overview

| Rule | Severity | What it checks |
| --- | --- | --- |
| [ARCCHR0001](./ARCCHR0001.md) | Error | Recognized aggregate event-handler candidates have supported signatures. |
| [ARCCHR0002](./ARCCHR0002.md) | Warning | Multiple candidate command identities without an explicit provider/recognized return exemption. |
| [ARCCHR0003](./ARCCHR0003.md) | Warning | Reactor access to the default event log instead of returned side effects. |
| [ARCCHR0004](./ARCCHR0004.md) | Warning | Redundant explicit id on `[EventType]`: empty, or equal to the type name. |
| [ARCCHR0005](./ARCCHR0005.md) | Warning | Chronicle usage and `AddCratisArc` appear in one project without integration setup. |
| [ARCCHR0006](./ARCCHR0006.md) | Warning | Manual reactor command execution without method- or class-level `[OnceOnly]`. |
| [ARCCHR0007](./ARCCHR0007.md) | Warning | A command `Handle()` or `Provide()` parameter is `IEventLog` or an implementing type. |
| [ARCCHR0008](./ARCCHR0008.md) | Warning | Data annotations `[Key]` used where Chronicle key resolution applies. |
| [ARCCHR0009](./ARCCHR0009.md) | Warning | Likely secret command values lack audit exclusion metadata. |
| [ARCCHR0010](./ARCCHR0010.md) | Warning | A keyless command returns a raw Guid beside statically identifiable untargeted events. |
| [ARCCHR0011](./ARCCHR0011.md) | Info | Under Off, plain read-model parameters or `IReadModels.GetInstanceById` calls in event-producing command or validator code. |
| [ARCCHR0012](./ARCCHR0012.md) | Info / Warning under On | Immediate appends beside protected decisions or under On. |
| [ARCCHR0013](./ARCCHR0013.md) | Info | A command or aggregate root names an event source with `[EventSourceType]` and a definition with that name exists in the compilation. |
| [ARCCHR0014](./ARCCHR0014.md) | Warning | A reactor or reducer is filtered with `[FromEventSource<T>(stream)]` to a stream the definition does not declare. |
| [ARCCHR0015](./ARCCHR0015.md) | Warning | A model-bound command handler declares a nullable event, directly or in a supported awaitable/union branch. |
| [ARCCHR0016](./ARCCHR0016.md) | Warning | `concurrency: true` on a stream-metadata attribute of a command whose `Handle()` returns `EventsWithConcurrencyScopes`. |
| [ARCCHR0017](./ARCCHR0017.md) | Error | An `[EventStreamId]` template on a command names an unusable property or has malformed braces. |
| [ARCCHR0018](./ARCCHR0018.md) | Warning | An `[EventStreamId]` template on a reactor, where it is used literally. |
| [ARCCHR0019](./ARCCHR0019.md) | Warning | A conventional query resembling a read-model fold; discovery is unchanged. |
| [ARCCHR0020](./ARCCHR0020.md) | Warning | A literal wrapper route names a stream its known definition does not declare. |
| [ARCCHR0021](./ARCCHR0021.md) | Info | Legacy wrapper strings name a declared stream but drop its definition. |
| [ARCCHR0022](./ARCCHR0022.md) | Warning | A command-key aggregate accompanies returned or appended events targeting a generated parameter. |
| [ARCCHR0023](./ARCCHR0023.md) | Warning / Info | Distinguishing read-model keys with several or no matching command properties; repeated lookup keys. |
| [ARCCHR0024](./ARCCHR0024.md) | Warning | Statically refused read shapes in Measure or On. |
| [ARCCHR0025](./ARCCHR0025.md) | Warning / Info | Read-model or explicit Chronicle service dependencies in keyless event-producing commands. |
| [ARCCHR0026](./ARCCHR0026.md) | Info / Warning under On | MongoDB collection parameters containing recognizable Chronicle-backed models. |
| [ARCCHR0027](./ARCCHR0027.md) | Warning | A visible `OnRefused` callback returns a statically invalid refusal production. |
| [ARCCHR0028](./ARCCHR0028.md) | Warning | Raw Chronicle reads and explicit read bypasses under On. |
| [ARCCHR0029](./ARCCHR0029.md) | Error | Conflicting protected and advisory declarations. |
| [ARCCHR0031](./ARCCHR0031.md) | Error | Invalid explicit LookUpBy property or non-model parameter. |
| [ARCCHR0032](./ARCCHR0032.md) | Info | Inert or redundant declarations. |
| [ARCCHR0033](./ARCCHR0033.md) | Warning | Aggregate and protected plain read on the same key under On. |
| [ARCCHR0034](./ARCCHR0034.md) | Info | Method-level advisory does not cover read parameters. |
| [ARCCHR0035](./ARCCHR0035.md) | Warning | Early aggregate commit under On. |
| [ARCCHR0036](./ARCCHR0036.md) | Warning | Exact scope coverage of a protected read is not proven, including validator-first stream reads. |
| [ARCCHR0037](./ARCCHR0037.md) | Error | Per-stream model parameter does not match the command route, or uses LookUpBy. |
| [ARCCHR0038](./ARCCHR0038.md) | Info | Per-stream model guard widens through another read or declared concurrency. |
| [ARCCHR0039](./ARCCHR0039.md) | Info | Prefer plain read-model parameters to decision readers or tokens in Handle. |

A command that has `[EventStreamId(value)]` and also implements `ICanProvideEventStreamId` is reported by Chronicle's own analyzer as CHR0027, not by an Arc rule. `[EventStreamId("")]` and `[EventStreamId(null)]` declare no id, so ARCCHR0017 stays silent for them.

## Quick fixes

| Rule | Fix |
| --- | --- |
| [ARCCHR0008](./ARCCHR0008.md) | Rewrite to the fully qualified Chronicle Key attribute. |
| [ARCCHR0010](./ARCCHR0010.md#quick-fix) | Compiler-checked local replacement with `EventSourceId<Guid>` for supported direct tuple syntax. |
| [ARCCHR0013](./ARCCHR0013.md) | Replace matching source/stream attributes with a typed event source definition. |
| [ARCCHR0031](./ARCCHR0031.md) | Use nameof for a nearby command lookup property. |
| [ARCCHR0021](./ARCCHR0021.md#quick-fix) | Replace legacy wrapper route strings with the matching definition and stream. |
| [ARCCHR0015](./ARCCHR0015.md#quick-fix) | Compiler-checked replacement of a nullable event with an explicit event-or-validation result and rejection placeholders. |
| [ARCCHR0016](./ARCCHR0016.md#quick-fix) | Remove `concurrency: true` from the attribute. |
| [ARCCHR0019](./ARCCHR0019.md#quick-fix) | Preview whole-type explicit query selection, retaining intended endpoints. |

The other rules have no automatic fix. A fix can change behavior: in particular ARCCHR0010 changes the persistence target when the Guid was an ordinary response.

## Installation

Install the optional Arc integration package:

```bash
dotnet add package Cratis.Arc.Chronicle
```

It brings in `Cratis.Arc.Chronicle.CodeAnalysis`, which supplies these analyzers and code fixes. Complete the [integration setup](../cratis-package.md) to connect Arc's command pipeline to Chronicle.

Pure `Cratis.Chronicle` provides event sourcing without Arc; referencing it alone does not install Arc's integration diagnostics. If a diagnostic is missing, check that your application's resolved dependencies include `Cratis.Arc.Chronicle.CodeAnalysis`.
