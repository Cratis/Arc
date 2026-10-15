---
title: Scenarios for rules that hold
description: A catalogue of rule shapes from the simplest to the most advanced, each with a complete example, the guard it produces, and what does not hold.
---

Use this page when you know the shape of your rule (see the [decision matrix](../index.md#decision-matrix-by-rule-shape)) and want the code. Every scenario states the rule, shows a complete example under read protection `On`, and says what holds and what does not. Each example shows its guard as one line. [How it works](../how-it-works.md) explains the notation.

## Fully idiomatic Arc

Start here. These scenarios use `[Command]` records that return events, constraints, validators with read models, stream decisions and aggregates.

| Rule | Mechanism | Page |
| --- | --- | --- |
| A book cannot be borrowed when no copy is left | A read model parameter and a validator | [One entity](./single-entity.md) |
| One library card per member. A username is unique | `[Unique]` on an event or a property | [Unique and once](./unique-and-once.md) |
| A member may hold three loans, and the last copy of a book | Two read models, one batch | [Two entities](./two-entities.md) |
| A course has capacity and a student has a limit | Two read models, two events, atomic | [Two entities](./two-entities.md#a-course-and-a-student) |
| At most 5000 may be withdrawn per account per month | Per-stream read model, or a stream read in `Provide()` | [One stream](./per-stream.md) |
| A report is approved once per owner per month | A stream decision | [One stream](./per-stream.md#a-period-approval) |
| Steps of a process happen in order and a finished process accepts nothing more | A stream with declared concurrency, and a closed stream | [Process stream](./process-stream.md) |
| Invoice numbers have no gaps and no duplicates | A numbering source read in `Provide()` | [Gapless numbers](./numbering.md) |
| Many rules over one lifecycle | An aggregate root | [Rich aggregate](./aggregate.md) |
| Do not overwrite changes the user did not see | An explicit scope with the revision | [Editing conflict](./editing-conflict.md) |
| A rare violation, a hint, external data, a hot key | No guard, or the right shape of data | [Rules that need no guard](./no-guard.md) |

## Choose a stream or source model

| Rule lives in… | Use |
| --- | --- |
| Folded state of one stream on the command's own route | [Per-stream model](./per-stream.md) |
| The entity's whole history, such as a balance | Whole-source model. Combining it with a per-stream model gives a whole-source guard |
| “Has fact X happened in this short stream?”, without a model | Stream decision |
| Rich behaviour across the whole entity | Aggregate, without reads of the same source |
| Another month or another source | `IDecisionReads` stream overload in `Provide()` |
| Uniqueness across sources | Constraint |

## Less idiomatic and Chronicle direct

These work, and Arc supports them. Under `On` some are refused; the pages say which.

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
