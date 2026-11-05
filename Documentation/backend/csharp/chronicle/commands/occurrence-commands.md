---
title: Run commands for Chronicle occurrences
description: Execute Arc commands for Chronicle occurrences and store receipts with their events so redelivery reuses the recorded outcome instead of repeating work.
---

Use `IOccurrenceCommandExecutor` to run an Arc command for a Cratis Chronicle occurrence and commit its events with a receipt. An occurrence is work delivered without an HTTP request, such as a schedule becoming due or the application starting. If delivery repeats after a crash, restart or lost lease, the receipt lets the executor return the recorded outcome without running the handler again.

Concurrent attempts can enter the handler before either commits. Only the winning attempt's events and receipt are kept; external effects such as sending an email need their own idempotency key to prevent duplication.

The executor is part of `Cratis.Arc.Chronicle` and requires a kernel advertising `effect-receipts`. Its Chronicle client APIs are available in 19.81 and later.

## How it works

An effect key identifies one named piece of work within an occurrence. Choose a stable effect name, such as `send-reminder`, and reuse it across delivery attempts. The key also carries the writing lease, but equality depends only on the occurrence id and effect name; a new lease does not create a new effect.

An effect receipt records the settled outcome. When a command appends events, the receipt is written in the same atomic commit. A later attempt that finds the receipt skips execution. If attempts race before that lookup can find it, commit selects the winning receipt and discards the other attempt's pending events.

For each call the executor:

1. Open a service scope for the occurrence's namespace and verify the tenant mapping resolves to that namespace.
2. Look up the effect receipt and return its stored outcome if one exists.
3. Run the command as the system with only the supplied roles, a fresh correlation id, and an `Occurrence` entry on the causation chain, which records what caused the command.
4. Settle a successful command by committing its unit of work with the receipt, or recording a receipt alone when there are no events. A concurrent winner's receipt takes precedence.

## Execute a command for an occurrence

```csharp
public class SendReminders(IOccurrenceCommandExecutor executor) : IOccurrenceHandler<ReminderDue>
{
    public async Task Handle(ReminderDue due, OccurrenceContext occurrence, CancellationToken cancellationToken)
    {
        var result = await executor.Execute(
            new SendInvoiceReminder(due.InvoiceId),
            occurrence,
            "send-reminder",
            ["Scheduler"],
            cancellationToken);

        if (!result.IsSettled)
        {
            // Nothing was recorded. Throw so the occurrence is redelivered and the effect is attempted again.
            throw new ReminderAttemptFailed(due.InvoiceId);
        }
    }
}
```

`ReminderDue`, `SendInvoiceReminder` and `ReminderAttemptFailed` are the application's own types. The overload without roles runs as the system with no roles; name the roles the command's authorization needs. Nothing is inherited from a caller.

## What the result tells you

| Disposition | Meaning | What to do |
| --- | --- | --- |
| `Recorded` | This attempt wrote the receipt. The effect is settled. | Acknowledge the occurrence. |
| `AlreadyRecorded` | A receipt already existed. `Outcome` and `Response` are the stored ones. | Acknowledge the occurrence. |
| `Failed` | A later attempt can succeed. No receipt, nothing committed. | Let the occurrence be redelivered. |

`Outcome` is `Appended`, `NoEvents` or `Refused`. `CommandResult` carries the command's own result when this attempt ran it.

### What is terminal and what is retried

| Command outcome | Receipt | Disposition |
| --- | --- | --- |
| Events committed | Written atomically with the events | `Recorded` (`Appended`) |
| Succeeded without events | Written (`NoEvents`) | `Recorded` |
| Validation failure, business refusal, constraint violation | Written (`Refused`) | `Recorded` — an operator reruns with a new effect key |
| Concurrency conflict or a protected read that changed | None | `Failed` |
| Handler exception or infrastructure failure | None | `Failed` |
| Authorization failure | None | `Failed` |

A recorded refusal is a settled business decision, not a request to keep trying the same effect. Redelivery returns that refusal even if the underlying state later changes. After an operator fixes the cause, use a new effect name to authorize another execution. A concurrency conflict or infrastructure failure leaves no receipt, so redelivery can attempt the same effect again.

## The stored response

The receipt stores a minimal plaintext response: the command type name, the outcome, the number of events, and for a refusal the category and member or constraint names. It never serializes command property values or the handler's response values. Validation messages are also excluded because they can echo personal data or secrets. Keep member and constraint names free of personal data.

Command names and individual reasons are capped at 256 characters, with at most 16 reasons plus an omission marker. Truncated strings end in `[truncated]`. These limits keep the serialized response below 64 KiB even when JSON escapes every character. The same bounded refusal reasons are used in the receipt's outcome.

A personal-data annotation or an oversized handler response does not by itself block keyed execution, because neither response values nor command values enter the receipt. If a redelivered handler needs the command's response, query the state produced by its events. The receipt cannot replay that response.

## What a command cannot do inside an occurrence

One receipt covers one unit of work and must settle with its final commit. Start the executor from the occurrence handler, not from a command or reactor that already owns a unit of work. Commands returned or nested by the executed command share that unit and the same effect key.

These operations break that boundary and are refused:

| Operation | Error |
| --- | --- |
| Calling the executor inside another command | `OccurrenceCommandCannotRunInsideCommand` |
| Calling an aggregate root's `Commit()` early | `OccurrenceCommandCannotCommitEarly` |
| Completing a stream outside the atomic commit | `OccurrenceCommandCannotCompleteStreams` |
| Appending immediately through a built-in event sequence | `ImmediateAppendRefused` |

For early commit, stream completion and immediate-append refusals, the executor rethrows the error even when the pipeline captured it in a `CommandResult`. These are programming errors, not retryable `Failed` dispositions, and no receipt is written.

Calling the executor from inside a command directly throws `OccurrenceCommandCannotRunInsideCommand`. If an occurrence-executed command makes that call and the exception reaches the pipeline, the pipeline captures it and the outer executor returns `Failed` without writing a receipt. Move the executor call to the occurrence handler rather than retrying the same nested call.

Return events or use `.Transactional` on the command's unit of work instead of appending immediately. Chronicle's built-in event sequences refuse immediate appends before sending anything in the command's async flow; pending transactional events are rolled back. The restriction ends when the invocation exits and does not affect later commands outside it.

A custom `IEventSequence` must honor Chronicle's append-admission contract. The executor's post-commit detection on the command's event log is only a backstop: it cannot prevent a custom implementation's immediate write or cover other sequence instances. See [transactional commands](./transactional-commands.md) for the append boundary.

## Fail closed without the kernel capability

If `effect-receipts` is absent or unknown, the executor throws Chronicle's `EffectReceiptsNotSupported` before running the command. There is no fallback that executes the command without a receipt.

## Occurrence causation and time

The causation chain of every event the command appends includes an `Occurrence` entry before the command's entry. A root or process entry can come first. The occurrence entry carries `occurrenceId`, `triggerId`, `kind`, `recordedAt`, `dueAt` for scheduled occurrences, `attempt`, `epoch` and `effectKey`, followed by the usual [command causation](./causation.md). Every attempt gets a fresh correlation id. The event's `Occurred` time is the time it was appended, not the due time of the occurrence; read `dueAt` from the causation when you need it.

## Namespaces and tenants

The occurrence's namespace selects the tenant: the default namespace is the default tenant, any other namespace is a tenant of that name. The executor then verifies that the event store resolves to the same namespace and throws `OccurrenceCommandNamespaceMismatch` when your tenant mapping disagrees. An occurrence without a namespace runs in the default namespace.

## Testing

Test an occurrence handler against the in-process kernel from `Cratis.Chronicle.Testing`: its capability switch makes the kernel advertise or hide `effect-receipts`, and it stores receipts so a second execution of the same effect key returns `AlreadyRecorded`. See [Testing with Chronicle](../../testing/chronicle.md#commands-for-occurrences).

## Things to know

- The effect name must be stable across attempts and unique per effect inside one occurrence. Pass `occurrence.EffectKey(name).Value` to external systems that accept an idempotency key.
- An effect needing more than one unit of work needs more than one effect name; each name has its own receipt.
- Receipt replay carries only the minimal outcome, never a command's response values. The receipt remains plaintext.

If the work starts from an event reactor rather than an occurrence, use [command side effects](../reactors/command-side-effects.md). For occurrence handlers, continue with [testing commands for occurrences](../../testing/chronicle.md#commands-for-occurrences) to check first delivery, redelivery and retryable failure.
