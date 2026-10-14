---
title: Frequently asked questions
description: Short answers about making a command's rule hold with Arc and Chronicle - constraints, protected decisions, validators, costs, retries and testing.
---

Use this page for a quick answer. Each answer links to the page that explains it. The decision table is in [Choosing how a command's rule holds](./index.md).

## Choosing

### Does reading a read model in a validator or in `Handle()` make my rule hold?

No. An injected read model, or `IReadModels.GetInstanceById<T>(...)`, is a plain read. It returns a snapshot, captures no boundary and guards nothing. Two commands that run at the same time can both read the same state and both succeed. See [validators](./validators.md#a-validator-never-guards-the-rule).

### How do I make a unique value hold?

Declare a constraint: `[Unique]` on an event property or class, or an `IConstraint`. The kernel enforces it for every writer. A check followed by an append has a gap that a constraint does not. See [a unique value](./scenarios.md#a-unique-value).

### How do I enforce a count, a stock level or a capacity?

Mark the command `[ProtectedDecision]` and take the state as a `DecisionRead<T>`. Chronicle captures the boundary before it folds the state, and rejects the append if a matching event arrived after it. A constraint cannot count. See [the idiomatic path](./index.md#the-idiomatic-path).

### How do I enforce a rule that spans two keys?

Read both keys with `IDecisionReads`, return one event per key, and let Chronicle append the batch atomically. Each read guards its own key. See [a rule spanning two keys](./scenarios.md#a-rule-spanning-two-keys).

### How do I handle two people editing the same document?

Return `EventsWithConcurrencyScopes` with the revision the client saw. A protected decision is not needed when the edit does not depend on state you read. See [an editing conflict](./scenarios.md#an-editing-conflict).

### When is a rare violation acceptable?

When the correction is cheap and harmless to the people affected, for example overbooking that a reactor cancels afterwards. Then use the default append and a reactor that appends a correcting event. A reactor cannot undo an irreversible side effect. See [a rare violation is acceptable](./scenarios.md#a-rare-violation-is-acceptable).

### Should I use an aggregate or a protected decision?

An aggregate fits rich behaviour on one entity. It guards its own event source and the event types it handles, has no snapshots, does not guard a rule across aggregates, and cannot be combined with `[ProtectedDecision]` reads in one command. A protected decision fits a rule over flat state on one or more keys. See the [matrix](./index.md#decision-matrix-by-rule-shape).

### What does `concurrency: true` do?

It chooses which dimensions bound the check Arc builds for the returned events. That check reads the tail after `Handle()` returns, so it does not bind your decision to the state you read. See [pitfalls](./pitfalls.md#concurrency-true-is-not-the-guard) and [Concurrency](../commands/concurrency.md).

## Protected decisions

### Why is protection opt-in?

Unmarked commands keep the behaviour they had before protected decisions existed. A protected command fails closed: it refuses validators with constructor dependencies, read shapes it cannot guard and a direct commit of its unit of work. Turning that on for every command would turn working commands into errors. See [why protection is opt-in](./index.md#why-protection-is-opt-in).

### What happens if I forget `[ProtectedDecision]`?

Depends on the command. On an unmarked command, taking `DecisionRead<T>` is refused and the command fails. On a command marked `[Unprotected]`, `DecisionRead<T>` is an unguarded read and nothing fails. A plain read next to a protected decision is also unguarded. See [pitfalls](./pitfalls.md#a-decision-read-on-the-wrong-kind-of-command).

### Which read models can I take as a `DecisionRead<T>`?

A flat projection keyed by the event source, with an `Id` property of a string-like or `Guid` type, built from a known, finite set of event types, over the event log. Reducers, self-reducing models, joins, children, `FromEvery`, keys from event properties and open-ended event types are refused. A refused shape fails the command every time it runs. See [pitfalls](./pitfalls.md#a-decision-read-model-without-an-id).

### How do I find a refused shape before production?

Add `DecisionReadAdmissions` to your specs. It checks the `DecisionRead<T>` parameters of the `Handle` and `Provide` methods of `[ProtectedDecision]` commands in an assembly. A model read through `IDecisionReads.Get<T>(key)` inside `Handle` is not checked: give it its own `Admit<T>()` check or a scenario that runs the command. See [protected decision scenarios](../../testing/chronicle.md#check-that-chronicle-admits-the-decision-reads).

### Does Arc retry a command that conflicted?

No. The caller receives a validation result with the reason `concurrencyViolation`. The caller must read again and resubmit deliberately.

### Can I use a `CommandValidator<T>` on a protected command?

Yes, if it has exactly one public constructor and that constructor has no parameters. A validator with any constructor dependency is refused, and the command fails with an error response. See [what a protected command accepts](./validators.md#what-a-protected-command-accepts).

### Can I check a protected decision on the validate endpoint?

`{url}/validate` runs the filters and nothing else, so a successful validation does not promise that executing the command will commit. See [the validate endpoint](./validators.md#the-validate-endpoint).

### What does a decision read not guard?

`Revise` and `Redact` between the read and the append, a projection definition changed in another process, other event sequences, and external state. A guard detects every matching event appended after its boundary, whoever wrote it, but it protects only decisions that carry a guard: a writer that decides without one can still break the rule. See [where the guard stops](./pitfalls.md#where-the-guard-stops).

### Do decision reads work from other languages?

No. Decision reads exist in the .NET client only. Constraints are defined from .NET and bind every writer.

## Costs and performance

### What does a protected decision cost?

A decision read is a fresh fold of the key's history of the model's event types, on every read: about four calls per attempt, and up to three attempts when events arrive during the fold. It is linear in that history. One scratch run measured a decision read at about 1.6 to 2 times a cold passive fold and 4 to 6 times a materialized read, at 10 and 100 events. N=1,000 and N=10,000 were not measured. See [costs](./index.md#costs).

### How do I keep it cheap?

Keep the model flat and the key short-lived: a key per period, order or booking rather than one key for the life of the company. Use a constraint for uniqueness and a plain read for a hint. See [a hot key with a long history](./scenarios.md#a-hot-key-with-a-long-history).

## Failures

### Which failures are silent?

Most of the dangerous ones: a plain read treated as a guard, an immediate `IEventLog.Append` in a protected command, a command without an event source id property, an ignored `Commit()` result, the first append into a scope, a manual scope without a sequence number, and a model that folds a different key than the one the facts are written to. See [pitfalls](./pitfalls.md).

### Which analyzers help?

[ARCCHR0011](../code-analysis/index.md#arcchr0011-unguarded-legacy-decision-reads) and [ARCCHR0012](../code-analysis/index.md#arcchr0012-immediate-append-with-a-decision-read) are Info severity; [ARCCHR0007](../code-analysis/index.md#arcchr0007-command-handler-injects-ieventlog), [ARCCHR0002](../code-analysis/index.md#arcchr0002-ambiguous-command-identity), and [ARCCHR0016](../code-analysis/ARCCHR0016.md) are warnings. Raise ARCCHR0011 and ARCCHR0012 in `.editorconfig` for projects that rely on protected decisions. Several pitfalls have no analyzer.

### Why is the command's key important?

The command's event source id is the key of its decision read and the stream its events go to. A command with no id property gets a random id on every call, so nothing can conflict. A command with two candidate ids needs `ICanProvideEventSourceId`. See [no event source id, or two candidates](./pitfalls.md#no-event-source-id-or-two-candidates).

## Testing

### How do I test that a rule holds under concurrency?

Use `CommandScenario<T>().UseDecisionReads()`, seed real events with `Given`, and queue a competing event with `AppendConcurrently(...)`. It appends after the command read its state and before the command commits. A scenario without decision mode can queue competing appends for explicit scopes or stream decisions, but it does not exercise protected decision reads. See [protected decision scenarios](../../testing/chronicle.md#protected-decision-scenarios).

## Event source definitions and stream decisions

### Does an event source definition make my rule hold?

No. A definition gives the append a boundary: the event source id narrowed by the dimensions it declares. That boundary covers the whole source or stream whatever the event type, a coarse, aggregate-like boundary in DCB terms. Arc resolves the expected tail after `Handle()` returns, so the check cannot bind your decision to what you read. Use a constraint, a stream decision, a protected decision, an aggregate or an exact scope for that. See [what the definition guards](../commands/event-source-definitions.md#what-the-definition-guards).

### What is the difference between a definition, `[EventSourceType]` and an explicit concurrency scope?

| | `[EventSource<TSource>(stream)]` | `[EventSourceType]` and `[EventStreamType]` | Explicit scope (`EventsWithConcurrencyScopes`) |
| --- | --- | --- | --- |
| What it is | A typed definition with named streams and dimensions | Strings on the command | A scope you build from a revision you read |
| Validated | An undeclared stream fails; contradicting string attributes fail | No: any string is accepted | Not against a definition |
| Concurrency dimensions | Declared once on the definition | Chosen per command with `concurrency: true` | You state them in the scope |
| Binds the decision to what it read | No | No | Yes, as far as where you captured the revision |
| Wins over the others | Is the default | Overrides the definition when `concurrency: true` | Replaces every derived scope |

Use a definition for routing and the default boundary, a stream decision or an explicit scope when the decision depends on what it read. Existing string attributes remain supported on commands that declare no definition. See [migrate from string routing](../commands/event-source-definitions.md#migrate-from-string-routing).

### What is the difference between a stream decision and a decision read?

| | Stream decision (`IStreamReads`) | Decision read (`DecisionRead<T>`, `IDecisionReads`) |
| --- | --- | --- |
| Facts | Raw events in one routed stream | A projection folded from the events of one or more keys |
| Declared with | A route: a definition or string routing attributes, plus a stream id | `[ProtectedDecision]` on the command |
| Boundary | The stream's tail, captured before the read | The log's tail, captured before the fold, for the folded event types |
| Narrowed by event type | No: every event in the stream conflicts | Yes: the event types the model folds |
| Read timing | In `Provide()`, before `Handle()` | Before `Handle()` for a parameter or `Provide()`; during `Handle()` for `IDecisionReads.Get` in the body |
| Cost | The whole stream crosses the wire per command | A fresh fold of the key's history per read |
| Result | `decision.Append(...)` returns events with the exact scope | The event or result you return, guarded by the read |

A stream decision requires a specific event source id, stream type and stream id; otherwise it throws `StreamDecisionRequiresSpecificRoute`. Do not use a stream decision and a decision read on the same event source in one command: Chronicle refuses to combine an explicit scope with a decision read there.

Use a stream decision when the facts live in one short routed stream, and a protected decision for a rule over folded state or over several keys. See [stream decisions](../commands/stream-decisions.md) and [the decision matrix](./index.md#decision-matrix-by-rule-shape).

### Can one command guard two streams of the same event source?

Not with independent guards. `EventsWithConcurrencyScopes` carries one scope per event source id per command. Guard the whole stream type with `reads.StreamType(...)`, or split the command. See [keep one exact scope per source](../commands/stream-decisions.md#keep-one-exact-scope-per-source).

## Dynamic Consistency Boundary

### How does this relate to the Dynamic Consistency Boundary?

Chronicle expresses the DCB append condition: a concurrency scope plays the part of one query item with a position, and an append of several event sources is atomic. Chronicle does not use tags in a scope, so a fact about two entities is written as one event per entity. See [Dynamic Consistency Boundary](./index.md#dynamic-consistency-boundary), the [concept in Chronicle](/chronicle/dynamic-consistency-boundary/), [Chronicle's choosing guide](/chronicle/dynamic-consistency-boundary/choosing/), [its scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) and [its FAQ](/chronicle/dynamic-consistency-boundary/faq/). For where event source definitions and stream decisions fit, see [Route a command through an event source definition](../commands/event-source-definitions.md).
