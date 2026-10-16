---
title: Read protection FAQ
description: Short answers about read protection, its modes, validators, lookups, costs and testing, and about the other mechanisms that make a command's rule hold.
---

Use this page for a short answer. Each one links to the page that explains it.

## Choosing

### Does reading a read model in a validator or in `Handle()` make my rule hold?

Under `Off`, no. An injected read model is a plain read: a snapshot that guards nothing. Under `On`, yes: the parameter is read fresh and guarded, so the rule holds against concurrent commands for the event types the model folds. See [Getting started](./getting-started.md).

### How do I make a unique value hold?

A constraint. The kernel enforces it for every writer. See [Unique and once](./scenarios/unique-and-once.md).

### How do I enforce a count, a stock level or a capacity?

A read-model parameter under `On`, with the rule in a `CommandValidator<T>`. See [One entity](./scenarios/single-entity.md).

### How do I enforce a rule that spans two keys?

Take both read models as parameters, choose each key with `[LookUpBy]` where needed, and return one event for each entity in one batch. See [Two entities](./scenarios/two-entities.md).

### How do I handle two people editing the same document?

An explicit scope carrying the revision the client saw. See [Editing conflict](./scenarios/editing-conflict.md).

### Should I use an aggregate or a read model parameter?

An aggregate fits one entity with many rules and transitions. A read model with a validator fits a rule over state that is cheap to project, and a rule over several keys. An aggregate and a plain read of the same source in one command are refused. See [Rich aggregate](./scenarios/aggregate.md).

### When is a rare violation acceptable?

When the cost of the violation is lower than the cost of the guard and a reactor can repair it. See [Rules that need no guard](./scenarios/no-guard.md).

### When should I use a per-stream model?

| Rule lives in… | Use |
| --- | --- |
| Folded state of one stream on the command's own route | [Per-stream model](./scenarios/per-stream.md) |
| The entity's whole history, such as a balance | Whole-source model. Combining it with a per-stream model gives a whole-source guard |
| “Has fact X happened in this short stream?”, without a model | Stream decision |
| Rich behaviour across the whole entity | Aggregate, without reads of the same source |
| Another month or another source | `IDecisionReads` stream overload in `Provide()` |
| Uniqueness across sources | Constraint |

## Modes

### What are Off, Measure and On?

`Off` is today's behaviour and the default. `Measure` changes nothing and reports what `On` would do. `On` guards read-model parameters and refuses reads it cannot guard. See [Adopting read protection](./adopting.md).

### Where do I set the mode?

In the project: `<CratisReadProtection>On</CratisReadProtection>`. It is compiled into the assembly. A command can override it with `[ReadProtection(ReadProtectionMode.On)]`. There is no setting in `appsettings` that raises it.

### Can I change the mode at run time?

Only lower it, with `ReadProtection:MaximumMode`. See [Cap the mode at run time](./adopting.md#cap-the-mode-at-run-time).

### Why is protection opt-in?

A protected command fails closed, and existing commands read plain models in many places. Turning it on by default would turn commands that work into errors. See [Why protection is opt-in](./index.md#why-protection-is-opt-in).

### What does Measure cost?

One awaited tail read per command, with a timeout (250 ms by default), and background scans on a sample of commands (10 percent by default). No benchmark is published yet.

## Reads

### Which read models can I take as a parameter under On?

Flat, event-source-keyed projections with an `Id` of a string or `Guid` type, and unfiltered reducers or self-reducing models with no constructor dependencies other than `ILogger<T>`. Joins, children, by-property keys, stream-keyed projections, filtered reducers and Pinned delivery are refused. See [Reads](./reads.md).

### Why was my reducer refused?

It has a filter or constructor dependencies. Chronicle cannot replay a reducer exactly when it filters its events or depends on a service. Remove the filter or the dependencies, read a projection of the same events, or mark the read `[Unprotected("why")]`. See [Refusals](./refusals.md#read-model-parameters).

### How do I find a refused read before production?

Run the application in `Measure` and read `refused_read`, turn the build-time diagnostics up ([ARCCHR0024](../code-analysis/ARCCHR0024.md), [ARCCHR0028](../code-analysis/ARCCHR0028.md)), and run a scenario for every command under `On`.

### What does `[Unprotected]` do?

It declares a read advisory: it is read, and guards nothing. On a parameter or model type it covers that read. On a `Handle()` or `Provide()` method it covers the calls inside the body, not the parameters. On a command it turns read protection off for that command. See [Declare an advisory read](./reads.md#declare-an-advisory-read).

### What is `[LookUpBy]` for?

It names the command property that selects the instance a parameter reads. Use it when a command reads two models by different keys, or when the model's key is not the command's event source id. See [Choose the key with LookUpBy](./reads.md#choose-the-key-with-lookupby).

### What happens if I do not turn read protection on?

A plain parameter stays a plain read, as it always was. Nothing is guarded. A model declaring `[EventStream<TSource>(stream)]` still requires a matching command route and explicit stream id: an undefined stream read is refused in every mode. Models without that declaration retain their existing behavior. Set `<CratisReadProtection>On</CratisReadProtection>`, or move there through `Measure`.

### Do I still need `DecisionRead<T>` and `[ProtectedDecision]`?

Not under `On`: a plain parameter can replace `DecisionRead<T>`. A `[ProtectedDecision]` command with `DecisionRead<T>` keeps working. Adding `On` is not redundant: it also protects plain read-model parameters and validator constructor reads and changes conflict diagnostics. Outside `On`, `[ProtectedDecision]` is how a command opts in one at a time.

## Conflicts

### Does Arc retry a command that conflicted?

No. A guarded read that changed returns a `400` with the reason `concurrencyViolation` and the detail `ReadChanged`, and nothing is written. The client reads again and resubmits. See [Refusals](./refusals.md).

### What does a guard not cover?

`Revise` and `Redact`, projection definitions changed in another process, other event sequences, external state, and a writer that decides without a guard. See [How it works](./how-it-works.md#what-is-outside-the-guard).

### Why is the command's key important?

The key is the event source a read is guarded on, and the stream the events are written to. A command with no key gets a new random id for each call, so nothing can conflict. See [Pitfalls](./pitfalls.md#no-event-source-id-or-two-candidates).

## Validators

### Can I use a `CommandValidator<T>` for a state rule?

Yes, and it is the recommended place. Under `On` its read-model constructor parameters are guarded. Under `Off` it is a hint. See [Validators](./validators.md).

### Can I check the rule on the validate endpoint?

Yes. `{url}/validate` runs the validators with fresh reads. Nothing is guarded or written there, and a success does not promise that the command will succeed.

### Why is my validator refused under On?

Arc builds the validator for each command so its parameters belong to that command. A validator registered by hand as scoped, singleton, a factory or an instance is refused. Let convention discovery register it. See [Validators](./validators.md#what-a-validator-may-and-may-not-do-under-on).

## Costs

### What does a guarded read cost?

A fresh fold of the key's history, two tail reads, and a retry if events arrive during the fold. Keep models flat and keys short-lived. See [How it works](./how-it-works.md#what-it-costs).

### How do I keep it cheap?

Key by period, order or booking instead of by company; model one rule per projection; use a constraint for uniqueness; use `[Unprotected]` for hints.

## Failures

### Which failures are silent?

A uniqueness check without a constraint, reads under `Off`, `Measure`, `[Unprotected]` reads and unknown dependencies, an ignored aggregate commit, a command with no key, `concurrency: true` used as a guard, and writers that decide without a guard. See [Pitfalls](./pitfalls.md).

### Which analyzers help?

[ARCCHR0002](../code-analysis/ARCCHR0002.md), [ARCCHR0011](../code-analysis/ARCCHR0011.md), [ARCCHR0012](../code-analysis/ARCCHR0012.md), [ARCCHR0024](../code-analysis/ARCCHR0024.md), [ARCCHR0028](../code-analysis/ARCCHR0028.md), [ARCCHR0033](../code-analysis/ARCCHR0033.md), [ARCCHR0034](../code-analysis/ARCCHR0034.md), [ARCCHR0035](../code-analysis/ARCCHR0035.md) and [ARCCHR0036](../code-analysis/ARCCHR0036.md). The [index](../code-analysis/index.md) lists all rules.

## Testing

### How do I test that a rule holds under concurrency?

Use `new CommandScenario<T>()`, seed real events with `Given`, and queue a competing event with `AppendConcurrently(...)`. See [Test the race](./scenarios/testing.md).

## Event source definitions and stream decisions

### Does an event source definition make my rule hold?

No. A definition gives the append a boundary: the event source id narrowed by the dimensions it declares. Under `Off` Arc resolves the expected tail after `Handle()` returns, so the check cannot bind your decision to what you read. Use a constraint, a guarded read, a stream decision, an aggregate or an exact scope for that. See [what the definition guards](../commands/event-source-definitions.md#what-the-definition-guards).

### What is the difference between a definition, `[EventSourceType]` and an explicit concurrency scope?

| | `[EventSource<TSource>(stream)]` | `[EventSourceType]` and `[EventStreamType]` | Explicit scope (`EventsWithConcurrencyScopes`) |
| --- | --- | --- | --- |
| What it is | A typed definition with named streams and dimensions | Strings on the command | A scope you build from a revision you read |
| Validated | An undeclared stream fails; contradicting string attributes fail | No: any string is accepted | Not against a definition |
| Concurrency dimensions | Declared once on the definition | Chosen per command with `concurrency: true` | You state them in the scope |
| Binds the decision to what it read | No | No | Yes, as far as where you captured the revision |
| Wins over the others | Is the default | Overrides the definition when `concurrency: true` | Replaces every derived scope |

Use a definition for routing and the default boundary, a stream decision or an explicit scope when the decision depends on what it read. See [migrate from string routing](../commands/event-source-definitions.md#migrate-from-string-routing).

### What is the difference between a stream decision and a read model parameter?

| | Stream decision (`IStreamReads`) | Read-model parameter under `On` |
| --- | --- | --- |
| Facts | Raw events in one routed stream | A projection folded from the events of one key |
| Declared with | A route plus a stream id | The parameter |
| Boundary | The stream's tail, captured before the read | The log's tail, captured before the fold, for the folded event types |
| Narrowed by event type | No: every event in the stream conflicts | Yes: the event types the model folds |
| Cost | The whole stream crosses the wire per command | A fresh fold of the key's history per read |

A stream decision requires a specific event source id, stream type and stream id; otherwise it throws `StreamDecisionRequiresSpecificRoute`. A stream decision beside a whole-source read of the same source in one command is refused, because the stream scope is narrower than the read. See [stream decisions](../commands/stream-decisions.md).

### Can one command guard two streams of the same event source?

Not with independent guards. `EventsWithConcurrencyScopes` carries one scope per event source id per command. Guard the whole stream type with `reads.StreamType(...)`, or split the command. See [keep one exact scope per source](../commands/stream-decisions.md#keep-one-exact-scope-per-source).

## Dynamic Consistency Boundary

### How does this relate to the Dynamic Consistency Boundary?

Chronicle expresses the DCB append condition: a guard plays the part of one query item with a position, and an append of several event sources is atomic. Chronicle does not use tags in a guard, so a fact about two entities is written as one event per entity. See [Dynamic Consistency Boundary](./index.md#dynamic-consistency-boundary), the [concept in Chronicle](/chronicle/dynamic-consistency-boundary/), [Chronicle's choosing guide](/chronicle/dynamic-consistency-boundary/choosing/) and [its FAQ](/chronicle/dynamic-consistency-boundary/faq/).
