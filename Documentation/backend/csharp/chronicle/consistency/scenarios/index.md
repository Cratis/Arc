---
title: Choose an Arc consistency scenario
description: Choose a Cratis Arc consistency example by the rule you need to protect, from one entity or stream to gapless numbering and stale-edit rejection.
---

Choose a Cratis Arc consistency scenario by what must remain true when commands run at the same time. The examples below show which reads and writes belong together, the guard that checks for conflicting changes, and the limits of that protection.

If you have not chosen a mechanism, start with the [decision matrix](../index.md#decision-matrix-by-rule-shape). [How it works](../how-it-works.md) explains the one-line guard notation used in the examples.

## Fully idiomatic Arc

For a new command, start with the rule closest to yours. Most examples let Arc protect read-model parameters under read protection `On`; the stream and numbering pages explain when a direct read is needed.

| Rule | Mechanism | Page |
| --- | --- | --- |
| A book cannot be borrowed when no copy is left | A read model parameter and a validator | [One entity](./single-entity.md) |
| One library card per member. A username is unique | `[Unique]` on an event or a property | [Unique and once](./unique-and-once.md) |
| A member may hold three loans, and the last copy of a book | Two read models, one batch | [Two entities](./two-entities.md) |
| A course has capacity and a student has a limit | Two read models, two events, atomic | [Two entities](./two-entities.md#a-course-and-a-student) |
| At most 5000 may be withdrawn per account per month | Per-stream read model, or a stream read directly in `Handle()` with a reasoned ARCCHR0040 suppression | [One stream](./per-stream.md) |
| A report is approved once per owner per month | A stream decision | [One stream](./per-stream.md#a-period-approval) |
| Steps of a process happen in order and a finished process accepts nothing more | A stream with declared concurrency, and a closed stream | [Process stream](./process-stream.md) |
| Invoice numbers have no gaps and no duplicates | A numbering source read directly in `Handle()` with a reasoned ARCCHR0040 suppression | [Gapless numbers](./numbering.md) |
| Many rules over one lifecycle | An aggregate root | [Aggregate lifecycle rules](./aggregate.md) |
| Do not overwrite a title change the user did not see | An explicit scope with the revision the editor loaded | [Editing conflict](./editing-conflict.md) |
| A hint, a repairable violation, an external price, or a long-lived source | Leave advisory reads unguarded, or reshape the data that needs protection | [Advisory reads and shorter histories](./no-guard.md) |

## Choose a stream or source model

| Rule lives in… | Use |
| --- | --- |
| Folded state of one stream on the command's own route | [Per-stream model](./per-stream.md) |
| State built from the entity's whole history, such as a balance | Whole-source model, which folds events across the source's streams. Combining it with a per-stream model gives a whole-source guard |
| “Has fact X happened in this short stream?”, without a model | Stream decision |
| Lifecycle rules across the whole entity | Aggregate, without reads of the same source |
| Another month or another source | `IDecisionReads` stream overload directly in `Handle()`, with a reasoned ARCCHR0040 suppression linked to the tracking feature |
| Uniqueness across sources | Constraint |

## Less idiomatic and Chronicle direct

If existing code injects Chronicle services or runs outside Arc commands, check these pages before relying on automatic protection. They show which paths can participate in a guarded commit and which are refused under `On`.

| You write | Page |
| --- | --- |
| An injected `IEventLog`, `IReadModels` in `Handle()`, several event sources, controllers | [Less idiomatic Arc](./less-idiomatic.md) |
| `IEventStore`, `IEventLog` and `IDecisionReads` in endpoints, controllers and services | [Chronicle used directly](./chronicle-direct.md) |

## Operating it

| You want | Page |
| --- | --- |
| Move an existing application from `Off` to `On` | [Adopting read protection](../adopting.md) |
| Understand and fix a refusal | [Refusals](../refusals.md) |
| Test that a rule holds under concurrency | [Test the race](./testing.md) |

## Conventions

- The command's subject id is a type derived from `EventSourceId`, for example `public record Isbn(string Value) : EventSourceId(Value);`. Arc uses it as the event source id, so no `[Key]` is needed.
- References to other entities are `ConceptAs<T>`, unless the command appends to that entity too.
- A state rule goes in a `CommandValidator<T>` with the read model as a constructor parameter. `Handle()` returns the events.
- Examples on one page share one namespace, so the types declared in an earlier example are visible to later ones.
