---
title: How read protection works
description: What Arc does for each command under On, how a guard is derived from your reads, and how guards merge and widen.
---

Use this page when you want to know exactly which events make a command fail, or why a guard is wider than you expected. You do not need it to use read protection. [Getting started](./getting-started.md) is enough.

## What Arc does for a command

| Step | What happens |
| --- | --- |
| 1. Begin | Before the filters run, Arc starts the command's unit of work, claims ownership of it, and turns on read protection for the whole invocation. Nested commands share the same unit |
| 2. Resolve parameters | Each read-model parameter, in `Handle()`, `Provide()` and validator constructors, is read fresh through Chronicle's decision-read path and enrolled as a guard **before** your code sees the value |
| 3. Decide | Validators run, then `Provide()`, then `Handle()`. Reads of the same model and key inside one command share one fold |
| 4. Commit | Arc commits as the owner. Chronicle merges every guard into the conditions the kernel checks with the append |
| 5. End | Arc ends read protection after stream completions have run. A conflict writes nothing |

Chronicle's side of this is the [read-protection frame](/chronicle/read-models/read-protection/), and Arc is the host that drives it. If you use Chronicle directly, you drive it yourself.

## Where a guard comes from

A guard is an append condition: a position in the log plus a filter. The kernel rejects the append if an event that matches the filter was appended after the position.

| Part | For a read-model parameter |
| --- | --- |
| Event source | The key the parameter reads |
| Event types | The types the model folds, including the types that remove it |
| Position | The tail of the log, captured **before** the model was folded |

Capturing the position before the fold is what makes the guard sound. An event that lands during the fold, or after it, is after the position and is detected.

The examples in these pages show the guard as one line. It is a notation for the docs. `IUnitOfWork.GetAppendConditions()` returns the real conditions.

```text
// BookStock for isbn-1
guard isbn-1: types=[CopyAdded, BookBorrowed, BookReturned], after <tail before the read>
```

| Event arrives for | Conflicts? |
| --- | --- |
| The same book, one of the model's types | Yes |
| The same book, another type (a rename) | No |
| Another book | No |

A model that does not exist yet is guarded too. If no matching event existed at the read, an event of the model's types that arrives later conflicts. That is how "create it only if it does not exist yet" holds.

## Several reads

Reads of the same event source merge into one guard. The merged guard can only get wider, never narrower, so it never misses a change that either read would have caught.

| What the reads share | The merged guard |
| --- | --- |
| Same source | The union of the event types, from the earliest position |
| Different sources | One guard per source, validated together with the append |

```text
// Transfer: AccountBalance for acc-1 and AccountBalance for acc-2
guard acc-1: types=[Deposited, Withdrawn], after <tail before the first read>
guard acc-2: types=[Deposited, Withdrawn], after <tail before the second read>
```

Both guards are checked in the same atomic append. A conflict on either one rejects the whole batch.

## An explicit scope beside a read

If a command returns an exact scope (`EventsWithConcurrencyScopes`) for a source it also reads, Chronicle checks that the scope covers the read:

- the scope's source, types and stream dimensions are each unset or at least as wide as the read's;
- the scope's position is the same as or earlier than the read's.

If it does, the scope is used. If not, the command fails with `ExplicitScopeDoesNotCoverReads`. A stream decision is an explicit scope, so a stream decision beside a whole-source read of the same source is refused, because the stream is narrower than the read. [Refusals](./refusals.md) has the fixes.

## Declared concurrency on a definition

An [event source definition](../commands/event-source-definitions.md) can declare which dimensions guard an append. Under `On`, Arc does not build the routing scope itself. It hands the declared dimensions to Chronicle, which merges them with the read at commit.

| Definition | Under `On` |
| --- | --- |
| Declares nothing | Adds nothing |
| Declares dimensions including the event source id | Merged into the read's guard. The declared dimensions are kept only where they equal the read's, and the event types become any type |
| Declares dimensions without the event source id | Kept as a separate guard, not tied to the read. It does not protect the state you read |

A definition that declares `Concurrency = EventSourceId | EventStreamType` makes the guard of a `Withdraw` command wider:

```text
// without the definition's dimensions
guard acc-1: types=[MoneyDeposited, MoneyWithdrawn], after <tail before the read>

// with the definition's dimensions merged in
guard acc-1: any type, after <tail before the read>
```

Declare `Concurrency` only for ledgers, numbering and approval streams, where every writer should see every other. For anything else it only adds conflicts. See [Chronicle: how guards are derived](/chronicle/dynamic-consistency-boundary/how-guards-are-derived/) for the full merge rules and the notation.

## Aggregates and stream decisions

| Dependency | Guard |
| --- | --- |
| Aggregate root | Enrolled when the aggregate is rehydrated: its source and the event types it handles. An aggregate and a plain read of the same source are refused |
| Stream decision | An explicit scope enrolled when the stream is read: the routed stream, any event type |
| Fresh-identifier check | An expected-empty guard on the new id |

## What is outside the guarantee

| Situation | What happens |
| --- | --- |
| A writer that decides without a guard | It can still break the rule. A guarded writer detects its event |
| `Revise` or `Redact` between the read and the append | Not detected. They do not move the tail. Chronicle refuses them inside a protected invocation |
| A projection definition changed in another process | Not detected until this client registers again. Quiesce commands during a projection deploy |
| Other event sequences, external state, other databases | Not guarded |
| A command that appends nothing | A guard that exists only because of a read sends nothing. No write means nothing to protect |
| Events that arrive after the commit | Not part of the decision |

## What it costs

Each guarded read makes two tail reads and a fold of the key's history, and retries the fold when events arrive during it, up to three attempts. The first read of a model type in a process also checks the projection definition. Identical reads inside one command share one fold. The kernel serves the fold through a projection that is shared by every key of a model type, so heavy use of one model type is serialized across keys and tenants.

No benchmark for read protection is published yet. Keep guarded models flat and keys short-lived.

## Where to go next

- [Reads](./reads.md): protected, advisory and refused reads.
- [Chronicle: how guards are derived](/chronicle/dynamic-consistency-boundary/how-guards-are-derived/): the full rules, with examples.
- [Chronicle: read protection](/chronicle/read-models/read-protection/): the API Arc drives.
- [Concurrency](../commands/concurrency.md): the scope Arc builds when read protection is off.
