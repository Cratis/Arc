---
title: Command operation execution and recovery
description: Look up operation signatures, ordering, compensation eligibility, backend diagnostics, cancellation, and supported command boundaries.
---

This reference describes `ICommandOperation` in `Cratis.Arc.Commands`. Start with [command operations](./index.md) for the design rationale or [implementing an operation](./implementing.md) for a complete example.

## Declaration contract

| Member | Contract |
| --- | --- |
| `ICommandOperation` | Opts the returned value into command operation processing. An immutable record is the normal authoring shape. |
| `Execute(...)` | Exactly one public, nongeneric instance method. |
| `Compensate(...)` | Optional public, nongeneric instance method describing an attempted operation's business reversal. |
| Method result | `void`, `Task`, or `ValueTask`; Arc awaits asynchronous methods. |
| Service parameter | Required dependency from the originating command's service provider. |
| `CancellationToken` parameter | Supplied by Arc; at most one per method. Execution and compensation use different cancellation lifetimes. |
| `CommandOperationFailure` parameter | Optional immutable failure context, accepted only by `Compensate()`. |

Value-returning methods, including `Task<T>` and `ValueTask<T>`, are not supported operation execution signatures. An execution receipt is not implicitly added to a response or event. Neither method returns more pipeline values.

Do not use `async void`, static or generic methods, overloads, a service-locator parameter, optional service arguments, or by-reference parameters. Do not capture services inside the operation record. Business inputs belong in its properties; execution dependencies belong in method parameters.

Operations may be reference or value types. Nullable value-type operations and nullable batches remain server-only when present; null means absent. Prefer `[]` or `default(CommandOperations)` when expressing an empty batch rather than introducing a nullable batch.

The build diagnoses invalid conventions and generates typed invocation metadata. [ARC0016](../../code-analysis/index.md#arc0016-command-operation-methods) checks method shapes, [ARC0017](../../code-analysis/index.md#arc0017-command-operation-batches) rejects bare operation collections, and [ARC0018](../../code-analysis/index.md#arc0018-command-operation-visibility) checks generated-invoker accessibility.

Validated reflection fallback supports source-free declarations where generated metadata is unavailable. Generated operation invocation is not a claim that the whole Arc host supports NativeAOT.

## Returned values

| `Handle()` result | Treatment |
| --- | --- |
| Concrete operation or `ICommandOperation` | Execute the concrete operation on the server. |
| Null singular operation | No invocation. |
| `CommandOperations` | Expand the explicit batch in order. |
| `default(CommandOperations)` or `[]` | Empty batch. |
| Tuple | Combine operations, other server-handled values, and at most one client response. |
| `Result` or `OneOf` | Process only the active alternative, including its tuple values. |
| Task/ValueTask-wrapped supported return | Await the handler, then classify the result. |
| Ordinary array or enumerable | Retains ordinary response-value classification; it is not a generic operation batch. |

`CommandOperations` copies/materializes membership once and rejects null elements. It is not mutable after construction. Descriptor immutability remains the application's responsibility.

Operations are exclusively server-consumed. A broad custom response-value handler does not get to execute the same operation a second time. The proxy generator excludes operations and batches from the client response contract. Use a statically meaningful signature rather than hiding operations behind `object` or an arbitrary container.

## Execution and failure ordering

1. Arc runs authorization, validation, `Provide()`, and `Handle()`.
2. It classifies the return graph and validates operation metadata and dependencies before the first operation starts.
3. It processes control values and other server-consumed values, including returned Chronicle event enrollment.
4. If the command is still successful, it invokes operations sequentially in declaration order.
5. It records an invocation immediately before entering its `Execute()`, after dependency resolution and cancellation checks.
6. An execution failure or cancellation prevents later operations from starting.
7. Required execution scopes complete. Arc obtains conservative commitment facts before deciding on recovery.
8. Eligible compensators run in reverse invocation order. A compensation failure does not erase the original failure or make the command successful.
9. Arc finalizes backend observations and clears a selected response when the command is unsuccessful.

The operation whose `Execute()` throws is a started invocation. Its receipt-independent compensator is eligible alongside earlier started operations. A descriptor never entered is not eligible. An exception during dependency preflight does not count as entering `Execute()`.

A direct call to `Handle()`, `Execute()`, or `Compensate()` does not invoke this orchestration. It is a normal C# method call. Use `ICommandPipeline` or [CommandScenario](../../testing/command-scenario.md) to exercise the framework behavior.

## Commit and recovery

Compensation is conditional on both command failure and the observed business boundary. A failed `CommandResult` alone does not establish that nothing committed.

| `CommandCommitDisposition` | Recovery policy on failure |
| --- | --- |
| `NoCommit` | No coordinated commit participant exists; attempt declared compensation. |
| `NotCommitted` | Coordinated changes are known not committed; attempt declared compensation. |
| `Committed` | Suppress automatic reversal of work associated with committed business facts. |
| `Unknown` | Do not guess or reverse automatically. Report indeterminate recovery. |
| `Mixed` | No blanket reversal of partially committed work. Report indeterminate recovery. |

With Chronicle, operations execute before automatic transaction completion. A known rejection of the returned-event transaction can permit compensation. A thrown commit with uncertain outcome cannot. Previously successful immediate appends and explicit commits cannot be rolled back as pending enrollment.

Scope completion still runs after operation failure. Compensation does not replace the real rollback of pending Chronicle events or attempt to un-append them.

Post-pipeline serialization, result delivery, process crashes, and arbitrary application writes outside returned operations are not covered. No operation journal survives a process crash. Indeterminate recovery is a report, not a promise that Arc scheduled a later attempt.

## Backend observations

Backend `CommandResult` exposes `Recovery` and `OperationOutcomes`. These are excluded from HTTP JSON. They do not change the TypeScript command-result contract, and operation payloads are not returned to the client.

`Recovery` is absent when operation processing does not participate. When present, `CommandRecoverySummary` contains:

| Property | Meaning |
| --- | --- |
| `CommitDisposition` | Observed commitment facts, independent of `IsSuccess`. |
| `Status` | Recovery decision and observed outcome. |
| `StartedCount` | Number of `Execute()` invocations entered. |
| `CompletedCount` | Number of `Execute()` methods that returned successfully. |
| `CompensatedCount` | Number of `Compensate()` methods that returned successfully. |
| `FailedCompensationCount` | Number of compensators that threw. |
| `UncompensatedCount` | Started work requiring recovery that was not observed to complete compensation. |

`CommandRecoveryStatus` is `NotNeeded`, `Completed`, `Incomplete`, `Suppressed`, or `Indeterminate`. `Completed` means the required callbacks returned; it does **not** prove that external history was atomically erased or that a retry is safe.

`OperationOutcomes` contains server-only per-invocation observations: `InvocationIndex`, `OperationType`, `ExecutionCompleted`, `Compensation`, and an optional `CompensationFailure` message. These observations do not contain the operation's business properties.

`CommandOperationCompensation` distinguishes `NotNeeded`, `Completed`, `Failed`, `NotAvailable`, `BudgetExpired`, and `Suppressed`. Missing compensation is observable rather than silently labeled rollback.

Use the command's existing `CorrelationId` with server diagnostics. Keep recovery messages and operation types in trusted diagnostics rather than exposing them as application-facing error copy.

## Optional failure context

`Compensate()` can request `CommandOperationFailure` when a specialized reversal needs execution observations. Ordinary operations do not need this argument.

| Property | Meaning |
| --- | --- |
| `InvocationIndex` | Zero-based index of this invocation. |
| `InvocationCompleted` | Whether its `Execute()` returned successfully. |
| `IsFailingInvocation` | Whether this invocation threw the original execution failure. |
| `Source` | Original failure phase: planning, response handling, execution, cancellation, or scope completion. |
| `CommitDisposition` | Commitment facts used for the recovery decision. |
| `ExceptionMessages` | Defensive snapshot of original exception messages, not a mutable `CommandResult`. |

A false `InvocationCompleted` is not proof that an external service made no change. Use provider-supported reversal semantics rather than skipping cancellation solely because an invocation threw. Context properties cannot remove a failure or turn the command into success.

## Cancellation and recovery budget

`Execute()` receives the command execution token. For HTTP execution this is normally the request-aborted token. Arc checks cancellation before starting another operation and forwards the token to the operation method.

`Compensate()` receives a separate token so a disconnected client does not immediately cancel cleanup. `CommandOperationOptions.CompensationTimeout` defaults to thirty seconds and is shared by the recovery attempt.

This is a host-configuration fragment; import `System`, `Cratis.Arc.Commands`, and `Microsoft.Extensions.DependencyInjection`:

```csharp
builder.Services.Configure<CommandOperationOptions>(options =>
    options.CompensationTimeout = TimeSpan.FromSeconds(10));
```

The budget is cooperative. A method that ignores cancellation is still awaited; Arc does not forcibly terminate it or dispose services underneath running code. Operations not entered before the recovery budget expires are reported rather than silently claimed compensated.

There are no automatic retries of execution or compensation. Retrying an uncertain request and creating a new logical attempt after confirmed compensation have different idempotency requirements. A correlation ID or tuple index is not a universal external idempotency key.

## Supported scope profile

The initial contract supports flat sequential commands, explicitly compatible execution scopes, and at most one deferred commit participant. Chronicle supplies its compatible integration. Nested operation-bearing commands, parallel/detached command participation, receipt signatures, and durable recovery are not supported.

Attempting a same-host nested command from `Execute()` rejects the outer operation batch even if the operation ignores the child's failed result. A nested command attempted from `Compensate()` is recorded as failed compensation; remaining eligible compensators still run. Calling `ICommandPipeline` from these methods does not bypass the flat-boundary contract.

Custom execution scopes opt in through `ICommandOperationExecutionScope`, which extends `ICommandExecutionScope`:

- `IsCommitParticipant` declares whether the scope coordinates a deferred business commit.
- `GetCommitDisposition(CommandContext)` reports authoritative facts even after a completion failure; return `Unknown` when facts are unavailable.
- A nonparticipant promises not to commit business changes.
- `Begin()` must not commit business work.

Do not mark a database-committing scope as a nonparticipant merely to satisfy validation. Undeclared scope behavior is not assumed safe. Existing scope completion order remains unchanged; a later scope failure can therefore follow a known commit and suppress compensation.

Unsupported operation boundaries are rejected rather than silently falling back to application-authored cleanup. Arbitrary service writes performed outside declared operations remain outside this contract.

## Storage integrations and custom-scope checks

| Integration | Operation boundary |
| --- | --- |
| Standalone Arc | No automatic database transaction. Operations execute registered application services. |
| Chronicle | Compatible deferred event commitment, with conservative observations for rejected, committed, and uncertain outcomes. |
| [EF Core](../../entity-framework/index.md) | No built-in operation commit participant. Context registration and read-model observation do not automatically coordinate operation writes or call `SaveChanges` for you. |
| [MongoDB](../../mongodb/index.md) | No built-in operation commit participant. Collection/session access and resilience behavior remain provider concerns. |

Do not combine independent EF and Chronicle commits under the single-participant profile and describe them as atomic. A provider-backed operation must retain its own ownership and idempotency guarantees. Database execution strategies and driver resilience may retry within a provider call even though Arc does not retry operations.

Before declaring a custom scope compatible, specify these cases against its actual provider:

- `Begin()` establishes its lifetime without committing business work; partial initialization does not make cleanup unsafe.
- `IsCommitParticipant` is stable and describes the scope's responsibility, not whether the latest result happened to succeed.
- A confirmed rejection or rollback reports `NotCommitted`; an unverified commit acknowledgment reports `Unknown`.
- A known successful commit remains committed when a later scope reports an error.
- A completion exception still leaves commitment observations available. `CommandResult.IsSuccess` and a generic "completed" flag are not substitutes for those facts.
- Recovery dependencies remain usable after scope completion. A still-live DI scope does not repair a failed transaction or database context; obtain usable recovery resources without losing the tenant or ownership context.
- Cancellation and resource disposal do not interrupt recovery or dispose dependencies underneath running callbacks.

Use the [Chronicle constraint example](../../testing/command-operations-with-chronicle.md) as a concrete test of a known rejection, not as proof that a different provider has identical transaction semantics.

## Troubleshooting

| Symptom | What to inspect |
| --- | --- |
| Calling `Handle()` produces an operation but performs no work | This is a direct decision call. Use `ICommandPipeline` or `CommandScenario` for framework execution. |
| No operations started | Check authorization, validation, `Provide()`, return classification, required services, declaration diagnostics, and scope compatibility before assuming `Execute()` ran. `Validate()` does not run operations. |
| `Recovery` is absent | HTTP/TypeScript results intentionally omit it. On a backend result, rejection before operation processing or an absent operation can also leave it unset. |
| A custom scope prevents execution | Implement the compatible scope contract only if the scope can honor it. Preserve real commit facts; do not report a database-writing scope as noncommitting. |
| Recovery is `Incomplete` | Inspect `OperationOutcomes` for a missing compensator, a thrown compensation, or an exhausted cleanup budget. Not all started work was observed to be compensated. |
| Recovery is `Suppressed` | Business changes are known committed. A later failure is not permission to undo operations associated with those facts. |
| Recovery is `Indeterminate` | Commitment is unknown or mixed. Reconcile with the storage/provider boundary; Arc has not scheduled a durable retry or reversal. |
| Cleanup continues after the request was canceled | Compensation has its own token. Its timeout is cooperative, so a provider that ignores cancellation can exceed the budget. |
| Work repeats even though Arc does not retry | Inspect caller retries and provider/driver resilience policies. A provider can retry inside one `Execute()` invocation. |
| An ignored nested-command failure still rejects the batch | The flat-boundary guard is intentional. Compose operation declarations instead of invoking another same-host command from an operation. |
| No cleanup occurs after a process crash | The journal is in-memory. Use a durable reactor/outbox/workflow when recovery must survive process loss. |

Use the command correlation ID and trusted backend observations for diagnosis. Do not infer successful rollback from a generic failed command result, or trigger automatic retries from `IsSuccess == false` alone.

For application-level coverage of the actual execution and recovery path, follow [testing command operations](../../testing/command-operations.md), the [failure-case recipes](../../testing/command-operation-failures.md), and the [Chronicle commit-rejection example](../../testing/command-operations-with-chronicle.md).
