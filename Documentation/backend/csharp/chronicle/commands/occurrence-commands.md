---
title: Commands for Chronicle occurrences
description: Execute transactional commands for Chronicle occurrences without an HTTP request, using atomic effect receipts to avoid repeating recorded effects.
---

A Chronicle occurrence is work the event store hands to your application when no one made a request: a schedule came due, the application started or stopped, a clock rule fired. An occurrence can be delivered more than once — a crash, a lost lease or a restart redelivers it. `IOccurrenceCommandExecutor` commits a transactional command's events and receipt atomically. A later delivery with the same effect key returns the recorded outcome without running the handler. Concurrent attempts can still enter the handler before one wins the commit; external side effects need their own idempotency keys.

It is part of `Cratis.Arc.Chronicle` and needs a Chronicle kernel that advertises the `effect-receipts` capability (client 19.77 and later).

## How it works

The executor identifies each command by an **effect key**: the occurrence id, an effect name you choose, and the lease that is writing. When the command succeeds, Chronicle records an **effect receipt** in the same atomic commit as the command's events. The receipt and the events either both exist or neither does, so a crash after the commit can never be followed by a second run.

For each call the executor:

1. Opens the scope of the occurrence's namespace and checks that the tenant mapping resolves to that same namespace.
2. Looks up an existing receipt for the key. If one exists, it returns the stored outcome without running anything.
3. Runs the command as the system with only the roles you pass, on a fresh correlation id, with an `Occurrence` entry on the causation chain.
4. Commits the command's unit of work with the receipt enrolled, and returns the winning receipt. If another attempt recorded one first, you get that stored outcome and nothing of this attempt is kept.

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

A refusal is terminal because retrying the same input would be refused again. To run the effect again after an operator has fixed the cause, use a new effect name.

## The stored response

The receipt stores a minimal plaintext response: the command type name, the outcome, the number of events, and for a refusal the category and member or constraint names. It never serializes command property values or the handler's response values. Validation messages are also excluded because they can echo personal data or secrets. Keep member and constraint names free of personal data.

Command names and individual reasons are capped at 256 characters, with at most 16 reasons plus an omission marker. Truncated strings end in `[truncated]`. These limits keep the serialized response below 64 KiB even when JSON escapes every character. The same bounded refusal reasons are used in the receipt's outcome.

Because the receipt does not store the command's response values, a personal-data annotation or an oversized handler response does not require up-front refusal of keyed execution. If you need the command's own response on redelivery, query the state the events produced; it is not replayed from the receipt.

## What a command cannot do inside an occurrence

One receipt covers one unit of work, and the receipt is written only by its final commit. The executor refuses these:

- **Running inside another command.** `OccurrenceCommandCannotRunInsideCommand` is thrown. Call the executor from the occurrence handler, not from a command or reactor that already owns a unit of work. Commands the command returns or nests share the unit and are covered by the same key.
- **Committing early.** An aggregate root `Commit()` throws `OccurrenceCommandCannotCommitEarly`. Even if the pipeline catches it in a `CommandResult`, the executor rethrows it to the occurrence handler; it is a programming error, not a retryable `Failed` result.
- **Completing a stream.** Stream completion is applied outside the atomic commit. The executor surfaces `OccurrenceCommandCannotCompleteStreams` to the occurrence handler, also rethrowing it when the pipeline captured it. No receipt is written.

Direct (non-transactional) appends inside an occurrence command are **not retry-safe**. Return events from the handler or append through the command's unit of work using `.Transactional`. An immediate event-log append is detected after it commits: the executor rolls back pending transactional events, writes no receipt and throws `OccurrenceCommandAppendedImmediately`. The immediate event remains committed and redelivery can duplicate it. This is a fail-loud backstop, not prevention; do not rely on it to observe other sequence instances. Up-front refusal requires the [Chronicle append-admission scope](https://github.com/Cratis/Chronicle/issues/5019) and [Arc's corresponding guard](https://github.com/Cratis/Arc/issues/3354) once that Chronicle release ships. See [transactional commands](./transactional-commands.md).

## Fail closed without the kernel capability

When the kernel does not advertise `effect-receipts`, or the capability is unknown, the executor throws Chronicle's `EffectReceiptsNotSupported` before running the command. It never runs a command it cannot make retry-safe.

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
- Event reactors that return commands are a different mechanism; see [command side effects](../reactors/command-side-effects.md).
