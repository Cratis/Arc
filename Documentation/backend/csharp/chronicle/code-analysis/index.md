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
| [ARCCHR0011](#arcchr0011-unguarded-legacy-decision-reads) | Info | Recognizable legacy read-model parameters or `IReadModels.GetInstanceById` calls in event-producing command/validator code. |
| [ARCCHR0012](#arcchr0012-immediate-append-with-a-decision-read) | Info | Direct immediate `IEventLog.Append*` in a command taking a protected decision read. |
| [ARCCHR0013](#arcchr0013-use-the-event-source-definition) | Info | A command or aggregate root names an event source with `[EventSourceType]` and a definition with that name exists in the compilation. |
| [ARCCHR0014](#arcchr0014-observed-event-stream-is-not-declared) | Warning | A reactor or reducer is filtered with `[FromEventSource<T>(stream)]` to a stream the definition does not declare. |
| [ARCCHR0015](./ARCCHR0015.md) | Warning | A model-bound command handler declares a nullable event, directly or in a supported awaitable/union branch. |

## ARCCHR0002: ambiguous command identity

Use one identity candidate or implement `ICanProvideEventSourceId`. Positional Chronicle `[Key]` and the matching property count as one candidate. Recognized explicit identity/wrapper return signatures can exempt the command.

This analyzer's candidate convention includes implicit conversions; runtime discovery does not generally do so. A conversion-only `ConceptAs<Guid>` is not a safe runtime identity declaration. Use `EventSourceId<Guid>` ancestry or a selected key. Neither a clean analyzer result nor a return exemption proves that input-time aggregate/read-model dependencies use your intended id. See [identity timing](../resolving-event-source-id.md).

## ARCCHR0007: command handler injects IEventLog

Prefer [returned events](../commands/events.md). This rule matches `IEventLog` (and implementing-type) parameters on `Handle()` and `Provide()`, including read-only use; it does not inspect whether they append. Deliberate exact-revision reads or advanced explicit transactional appends may warrant a narrow suppression. The API remains supported at runtime, with the boundaries in [Transactional commands](../commands/transactional-commands.md). A warning is not a runtime prohibition.

## ARCCHR0011: unguarded legacy decision reads

A plain Chronicle-backed read model in an event-producing command's `Handle`, `Provide`, or convention-discovered `CommandValidator<T>` does **not** guard the resulting decision. A direct `IReadModels.GetInstanceById` in those methods is likewise advisory, including an application extension method named `GetInstanceById` on `IReadModels` (for example one taking a typed `EventSourceId<T>` identity) and a rule lambda over the whole command such as `RuleFor(c => c).MustAsync(...)`. In `Handle` or `Provide`, mark the command `[ProtectedDecision]` and switch to `DecisionRead<T>` or `IDecisionReads` where the [shape is admitted](../read-models/injecting-into-commands.md#decision-reads-for-event-dependent-commands). A protected command refuses validators with constructor dependencies, so for a read in a validator the message advises moving it into `Provide` or `Handle` instead; in validators only constructor parameters are reported. `[Unprotected]` on the command acknowledges intentional legacy reads; on a parameter or method it only suppresses this diagnostic, not runtime behavior. A `GetInstanceById` call is reported wherever it appears in such a validator — constructor, rule lambda (for example inside `MustAsync`), helper method or field initializer — with the same validator advice. The analyzer recognizes event-producing handlers through the same wrappers the command pipeline unwraps: `Task`/`ValueTask`, `Result<…>`/`OneOf<…>` branches and tuple elements, at any nesting (for example `Task<Result<EventsWithConcurrencyScopes, ValidationResult>>`), and `object[]`, `IEnumerable<object>` or `IEnumerable<EventForEventSourceId>` event sets. The analyzer only recognizes explicit event return types, aggregate parameters, model-bound Chronicle attributes (on the type, or on a property or record parameter, such as `[ChildrenFrom<T>]` or `[Join<T>]`), and source-declared projection/reducer artifacts. Aliases, erased return types, indirect service calls and projections defined in external assemblies can be missed. It does not certify a command safe when quiet.

## ARCCHR0012: immediate append with a decision read

A direct `IEventLog.Append*` (also via `IEventStore.EventLog`) in a command that takes `DecisionRead<T>` or `IDecisionReads` writes immediately and cannot be undone if owner commit later conflicts. Return events or use the explicit `Transactional` style. This Info diagnostic matches direct calls, not helper chains or aliases, and does not flag `Transactional.Append`. It is advisory, not a substitute for the runtime ownership checks that refuse direct completion of a protected unit of work.

## ARCCHR0013: use the event source definition

`[EventSourceType("Account")]` (and `[EventStreamType("Transactions")]`) name an event source with a string. When a type in the same compilation is declared as that event source with `[EventSource]` and, if you name a stream, declares that stream with `[EventStream]`, the diagnostic suggests `[EventSource<AccountEventSource>("Transactions")]`, so the source, its stream and its concurrency dimensions come from one definition. It only reports a command or aggregate root, and only when a definition has exactly that name. A string with no matching definition, a stream the definition does not declare, and a type that already declares `[EventSource<T>]` are left alone. The legacy attributes keep working, so this is a suggestion and never a requirement. There is no code fix, because the definition and the attributes can differ in the concurrency flags the attributes carry, and rewriting them would change which writers conflict.

## ARCCHR0014: observed event stream is not declared

`[FromEventSource<AccountEventSource>("Transactions")]` filters a reactor or reducer to one stream of an event source definition. When the definition does not declare that stream with `[EventStream("Transactions")]`, no event can match and the observer would never be called, so the diagnostic reports it as a warning on the attribute. Only the definition's own attributes are read, so a definition in a referenced assembly is checked the same way as one in your project. A stream argument that is not a constant string, and a definition that is not resolved yet, are left alone.

## Quick fixes

| Rule | Fix |
| --- | --- |
| [ARCCHR0008](./ARCCHR0008.md) | Rewrite to the fully qualified Chronicle Key attribute. |
| [ARCCHR0010](./ARCCHR0010.md#quick-fix) | Compiler-checked local replacement with `EventSourceId<Guid>` for supported direct tuple syntax. |
| [ARCCHR0015](./ARCCHR0015.md#quick-fix) | Compiler-checked replacement of a nullable event with an explicit event-or-validation result and rejection placeholders. |

The other rules have no automatic fix. A fix can change behavior: in particular ARCCHR0010 changes the persistence target when the Guid was an ordinary response.

## Installation

Install the optional Arc integration package:

```bash
dotnet add package Cratis.Arc.Chronicle
```

It brings in `Cratis.Arc.Chronicle.CodeAnalysis`, which supplies these analyzers and code fixes. Complete the [integration setup](../cratis-package.md) to connect Arc's command pipeline to Chronicle.

Pure `Cratis.Chronicle` provides event sourcing without Arc; referencing it alone does not install Arc's integration diagnostics. If a diagnostic is missing, check that your application's resolved dependencies include `Cratis.Arc.Chronicle.CodeAnalysis`.
