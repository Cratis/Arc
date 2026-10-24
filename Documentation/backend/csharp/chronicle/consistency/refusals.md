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

Arc and Chronicle do not re-run the command; the fold may retry up to three times. Keep the user's input, read fresh state, and offer a deliberate resubmission.

## Retryable: 400

| Failure | When | Message |
| --- | --- | --- |
| `ReadChanged` | A guarded read changed before commit | `BookStock 'isbn-1' changed after BorrowBook read it (BookBorrowed at 1042). Nothing from this command was written; submit it again.` |
| `ReadIncomplete` | The fold of the read model did not reach the log position it needed after three attempts, because events arrived while it ran | `BookStock 'isbn-1' could not be read completely. Nothing was written; submit it again.` |

Both arrive as a validation result with the reason `concurrencyViolation`. The detail is `ReadChanged` or `ReadIncomplete`, so a client can tell them from other concurrency violations. A `ReadChanged` result also carries the read model and key in its `state`.

When Chronicle reports the first event that matched the guard after the read, the message names its event type and sequence number, and the `state` adds `conflictingEventTypeId`, `conflictingEventType` and `conflictingEventSequenceNumber`. Reads that share a guard are merged, so the event can belong to another read under the same guard. When Chronicle does not report it, the message ends at `read it.` and the `state` has only the read model and key.

A `[ProtectedDecision]` command outside `On` words the message differently: `BookStock 'isbn-1' changed after it was read. Read it again and resubmit.`

## A read cannot be guarded: 500

### Read-model parameters

Arc checks discoverable decision-read parameter shapes after `ApplicationStarted`, in the background, using Chronicle's admission rules. A refusal logs the command, model and reason at Critical level and stops the host when that command's effective read protection is `On`. On `[ProtectedDecision]` commands outside `On`, explicit `DecisionRead<T>` parameters produce a Warning without stopping the host; plain `Off` and `Measure` parameters are not checked.

`ReadProtection:StartupAdmissionCheck` accepts `Default` (the default behavior), `Log` (report without stopping) or `Off` (skip). Values are case-insensitive; missing or invalid values use `Default`. The `ReadProtection:MaximumMode` ceiling applies. Startup is not blocked: a few requests may be served before a refusal stops the host.

The check skips disabled Chronicle artifact discovery (`AutoDiscoverAndRegister=false`), an unavailable event store, and hosts without Chronicle. Unexpected admission exceptions warn rather than stop the host. It never materializes a model or enrolls a guard. Runtime checks remain necessary for per-key failures and method-body `Get<T>` calls, which startup cannot discover. A host without `UseCratisChronicle` may connect earlier than before when it declares protected parameter reads; `Off` preserves lazy connection timing.

| Failure | Cause | Fix |
| --- | --- | --- |
| `ReadCannotBeProtected` | A parameter's model has a shape Chronicle cannot guard exactly | See the reasons below |
| `AmbiguousReadModelLookup` | Several command properties have the model's `Id` type, and no `[LookUpBy]` chose one | Add `[LookUpBy(nameof(Property))]` to the parameter |
| `InvalidLookUpBy` | `[LookUpBy]` names a property the command does not have, or sits on a parameter that is not a read model | Name an existing public property |

`ReadCannotBeProtected` has this message. The description and the fix change with the cause:

```text
LendBook reads 'stock' (BookStock) through a reducer that filters its events, so read protection cannot guard it. If it is only context, mark it [Unprotected("why")]. Otherwise remove the reducer's event filter or read an unfiltered projection.
```

| Description | Fix |
| --- | --- |
| through a reducer whose handler or reader implementation does not support protected replay | Use Chronicle's built-in reducer reader with a discovered reducer handler |
| through a reducer with constructor dependencies | Remove dependencies other than `ILogger<T>`; self-reducing models are exempt from this constructor restriction |
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

Unfiltered reducers with no constructor dependencies other than `ILogger<T>` are admitted. Self-reducing models are exempt from the constructor restriction, not from the filter restriction.

[ARCCHR0024](../code-analysis/ARCCHR0024.md) reports the statically visible cases at build time.

### Other reads and appends

| Failure | Cause | Fix |
| --- | --- | --- |
| `ReadRefusedWhileReadsAreProtected` | A read through `IReadModels`, `IEventStore`, `IEventLog` or an event sequence | Take a read model as a parameter, use `IDecisionReads.Get` in `Provide()`, or mark the method `[Unprotected("why")]` |
| `ReadModelInjectedOutsideDecision` | A read model injected through dependency injection anywhere other than `Handle()` and validator constructors, for example into a command filter or a service | Take the model as a parameter of `Handle()` or of a `CommandValidator<T>` constructor, and move the rule there. If it is only context, mark the read-model type `[Unprotected("why")]` |
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
| `ConcurrentNestedCommandsUnderReadProtection` | A nested command started while another nested command of the same outer command was still running, and the outer command, the new command or any running one has read protection `On` | Run nested commands one after another |

### Declarations

| Failure | Cause | Fix |
| --- | --- | --- |
| `CommandCannotBeBothProtectedAndUnprotected` | `[ProtectedDecision]` and `[Unprotected]` on one command | Keep one |
| `ConflictingReadProtection` | `[ReadProtection(On)]` or `Measure` and `[Unprotected]` on one command | Keep one |
| `DiscoverableValidatorRefusedInProtectedDecision` | A validator under `On` that Arc did not construct for this invocation | Let convention discovery register the validator |

[ARCCHR0029](../code-analysis/ARCCHR0029.md) and [ARCCHR0031](../code-analysis/ARCCHR0031.md) report declaration errors at build time.

## How errors look to a client

A `400` is a command result with a validation result, like any validator failure. A `500` carries the refusal text in `exceptionMessages` only when `ArcOptions.ExposeExceptionDetails` is `true`. That option defaults to `true` in Development and `false` elsewhere. When it is `false`, Arc replaces exception messages and removes stack traces; inspect server logs using the correlation id instead. Avoid exposing exception details in production.

When details are exposed, Arc replaces Chronicle's generic subject "The command" with your command's name. See the [HTTP contract](../../../../http-contract.md).

Chronicle documents the same refusals at the API level in [Read protection in Chronicle](/chronicle/read-models/read-protection/#refusals-at-a-glance).

## Where to go next

- [Reads](./reads.md): what each read becomes.
- [Adopting read protection](./adopting.md): find refusals with `Measure` before they happen.
