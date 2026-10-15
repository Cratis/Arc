---
title: Refusals under read protection
description: Every error read protection can return, what causes it, whether to retry, and how to fix it.
---

Use this page when a command fails under read protection `On` and you want to know why and what to change. Read protection fails closed: when it cannot guard a read exactly, it refuses the command instead of running it unguarded.

## Two kinds of failure

| Kind | Status | What it means | What to do |
| --- | --- | --- | --- |
| **The read changed** | `400` | Someone wrote an event that matches what the command read, before it committed. Nothing was written | Submit the command again. The client reads fresh state and decides again |
| **The code cannot be guarded** | `500` | A configuration error in the command. Retrying does not help | Change the code. The message names the read and the fix |

Arc does not retry for you. A retry that blindly resubmits the same decision would defeat the guard.

## Retryable: 400

| Failure | When | Message |
| --- | --- | --- |
| `ReadChanged` | A guarded read changed before commit | `BookStock 'isbn-1' changed after BorrowBook read it. Nothing from this command was written; submit it again.` |
| `ReadIncomplete` | The fold of the read model did not reach the log position it needed after three attempts, because events arrived while it ran | `BookStock 'isbn-1' could not be read completely. Nothing was written; submit it again.` |

Both arrive as a validation result with the reason `concurrencyViolation`. The detail is `ReadChanged` or `ReadIncomplete`, so a client can tell them from other concurrency violations. A `ReadChanged` result also carries the read model and key in its `state`.

The `ReadChanged` message does not name the conflicting event. Chronicle does not report it yet.

A `[ProtectedDecision]` command outside `On` words the message differently: `BookStock 'isbn-1' changed after it was read. Read it again and resubmit.`

## A read cannot be guarded: 500

### Read-model parameters

| Failure | Cause | Fix |
| --- | --- | --- |
| `ReadCannotBeProtected` | A parameter's model has a shape Chronicle cannot guard exactly | See the reasons below |
| `AmbiguousReadModelLookup` | Several command properties have the model's `Id` type, and no `[LookUpBy]` chose one | Add `[LookUpBy(nameof(Property))]` to the parameter |
| `InvalidLookUpBy` | `[LookUpBy]` names a property the command does not have, or sits on a parameter that is not a read model | Name an existing public property |

`ReadCannotBeProtected` has this message. The description and the fix change with the cause:

```text
LendBook reads 'stock' (BookStock) through a reducer, so read protection cannot guard it. If it is only context, mark it [Unprotected("why")]. Otherwise read a projection of the same events, or wait for reducer support.
```

| Description | Fix |
| --- | --- |
| through a reducer | Read a projection of the same events. An unfiltered reducer or self-reducing model without constructor dependencies (other than `ILogger<T>`) is admitted |
| through a reducer with constructor dependencies | Remove the reducer's constructor dependencies |
| through a reducer that filters its events | Remove the reducer's event filter, or read an unfiltered projection |
| with joins or children | Read the parts as separate models |
| keyed by an event property | Append a fact to the entity the model is keyed by, or use a constraint |
| using Pinned generation delivery | Use Compatibility delivery for this projection |
| with an unsupported key type | Give the model a string or `Guid` `Id` |
| keyed by a stream | Use `IDecisionReads.Get` with an explicit source, stream type and stream id in `Provide()` |
| from another event sequence | Read a model of the event log |
| without exactly one backing definition | Register exactly one projection or admitted reducer for the model |
| with a mismatched registered definition | Register the current definition before executing commands |
| through IMongoCollection | Inject the read model instead of its collection |

[ARCCHR0024](../code-analysis/ARCCHR0024.md) reports the statically visible cases at build time.

### Other reads and appends

| Failure | Cause | Fix |
| --- | --- | --- |
| `ReadRefusedWhileReadsAreProtected` | A read through `IReadModels`, `IEventStore`, `IEventLog` or an event sequence | Take a read model as a parameter, use `IDecisionReads.Get` in `Provide()`, or mark the method `[Unprotected("why")]` |
| `DetachedReadWhileReadsAreProtected` | `IDecisionReads.GetDetached` | Use `Get` in `Provide()` |
| `ImmediateAppendWhileReadsAreProtected` | An append through `IEventLog.Append`, `AppendMany`, `Revise`, `Redact` or `CompleteStream` that writes at once | Return the event from `Handle()`, or use `IEventLog.Transactional.Append`. Return `CompleteStream` instead of calling it |
| `ReadAfterProtectedCommit` | A protected read after an aggregate's `Commit()` completed the unit | Read before committing, and return the result of `Commit()` last |
| `ReadOfSourceWithStagedEvents` | A read of a source after the command staged events for it | Decide once in the outer command and pass the value down |
| `ProtectedUnitOfWorkRequiresOwner` | Command code called `Commit()`, `Rollback()` or `Dispose()` on the unit | Return events. Arc owns the unit |

[ARCCHR0028](../code-analysis/ARCCHR0028.md), [ARCCHR0012](../code-analysis/ARCCHR0012.md) and [ARCCHR0035](../code-analysis/ARCCHR0035.md) report the statically visible cases.

An immediate append is refused for the whole invocation, including after an aggregate's `Commit()`. An immediate append is written before the commit and stays if the commit conflicts, so it cannot be part of a guarded decision.

### Scopes and aggregates

| Failure | Cause | Fix |
| --- | --- | --- |
| `ReadAndAggregateOnSameSource` | An aggregate and a plain read of the same event source in one command | Decide from the aggregate's state, read another source, or mark the plain read `[Unprotected("why")]` |
| `ExplicitScopeDoesNotCoverReads` | The command returns an exact concurrency scope for a source that is narrower than what it read, or captured after it | Widen the scope and capture its boundary before reading, or remove it |
| `DeclaredRoutingNotCoveredByExplicitScope` | An exact scope does not cover the concurrency the event source definition declares | Widen the scope, or remove the declared concurrency |
| `ConflictingContributionsOnLabel`, `ConflictingExplicitScopes` | Two different scopes or aggregates guard one event source | Keep one |
| `ReadProtectionRequiresTheDefaultConcurrencyStrategy` | A custom concurrency strategy | Use the default strategy |
| `ReadDependencyTargetMismatch` | A guard belongs to another event store, namespace or sequence than the unit | Read and write in the same store and namespace |
| `ReservedConcurrencyScopeLabel` | A scope label starts with `$read-protection/` | Use another label |
| `SourceLessReadNotSupported` | A read with no event source | Read a source-keyed model |

[ARCCHR0033](../code-analysis/ARCCHR0033.md) and [ARCCHR0036](../code-analysis/ARCCHR0036.md) report the static cases of the first two rows.

### Nested commands

| Failure | Cause | Fix |
| --- | --- | --- |
| `NestedEarlyCompletionUnderReadProtection` | A nested command committed the outer command's unit (an aggregate `Commit()`) | Commit in the outer command |
| `ConcurrentNestedCommandsUnderReadProtection` | The outer command started a nested command while another nested command was still running | Run nested commands one after another |

### Declarations

| Failure | Cause | Fix |
| --- | --- | --- |
| `CommandCannotBeBothProtectedAndUnprotected` | `[ProtectedDecision]` and `[Unprotected]` on one command | Keep one |
| `ConflictingReadProtection` | `[ReadProtection(On)]` or `Measure` and `[Unprotected]` on one command | Keep one |
| `DiscoverableValidatorRefusedInProtectedDecision` | A validator under `On` that Arc did not construct for this invocation | Let convention discovery register the validator |

[ARCCHR0029](../code-analysis/ARCCHR0029.md) and [ARCCHR0031](../code-analysis/ARCCHR0031.md) report declaration errors at build time.

## How errors look to a client

A `400` is a command result with a validation result, like any validator failure. A `500` is a command result with `exceptionMessages` that carries the message. Arc replaces Chronicle's generic subject "The command" with your command's name. See the [HTTP contract](../../../../http-contract.md).

Chronicle documents the same refusals at the API level in [Read protection in Chronicle](/chronicle/read-models/read-protection/#refusals-at-a-glance).

## Where to go next

- [Reads](./reads.md): what each read becomes.
- [Adopting read protection](./adopting.md): find refusals with `Measure` before they happen.
