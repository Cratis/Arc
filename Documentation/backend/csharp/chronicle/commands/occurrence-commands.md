---
title: Commands for Chronicle occurrences
description: Execute a command from a Chronicle occurrence — a timer, startup, shutdown or clock automation — without an HTTP request, so that a redelivered occurrence never runs a successful command twice.
---

A Chronicle occurrence is work the event store hands to your application when no one made a request: a schedule came due, the application started or stopped, a clock rule fired. An occurrence can be delivered more than once — a crash, a lost lease or a restart redelivers it. `IOccurrenceCommandExecutor` runs a command for an occurrence so that a successful command runs exactly once, however many times the occurrence arrives.

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

The receipt stores a minimal plaintext response — the command type name, the outcome, the number of events, and for a refusal the category and the member names. It never contains command values, validation messages or the handler's response, so it holds no personal data and stays well below Chronicle's 64 KiB limit. If you need the command's own response, query the state the events produced.

## What a command cannot do inside an occurrence

One receipt covers one unit of work, and the receipt is written only by its final commit. The executor therefore refuses these:

- **Running inside another command.** `OccurrenceCommandCannotRunInsideCommand` is thrown. Call the executor from the occurrence handler, not from a command or reactor that already owns a unit of work. Commands the command returns or nests share the unit and are covered by the same key.
- **Committing early.** An aggregate root `Commit()` throws `OccurrenceCommandCannotCommitEarly`.
- **Completing a stream.** Stream completion is applied outside the atomic commit. `OccurrenceCommandCannotCompleteStreams` is thrown.
- **Appending immediately.** A direct append to an event sequence is committed without the receipt. The executor detects it after the command ran, rolls back the unit, writes no receipt and throws `OccurrenceCommandAppendedImmediately`. Redelivery would append those events again, so fix the handler: return the events or use the transactional style. See [transactional commands](./transactional-commands.md).

## Fail closed without the kernel capability

When the kernel does not advertise `effect-receipts`, or the capability is unknown, the executor throws Chronicle's `EffectReceiptsNotSupported` before running the command. It never runs a command it cannot make retry-safe.

## Occurrence causation and time

The causation chain of every event the command appends starts with an `Occurrence` entry carrying `occurrenceId`, `triggerId`, `kind`, `recordedAt`, `dueAt` for scheduled occurrences, `attempt`, `epoch` and `effectKey`, followed by the usual [command causation](./causation.md). Every attempt gets a fresh correlation id. The event's `Occurred` time is the time it was appended, not the due time of the occurrence; read `dueAt` from the causation when you need it.

## Namespaces and tenants

The occurrence's namespace selects the tenant: the default namespace is the default tenant, any other namespace is a tenant of that name. The executor then verifies that the event store resolves to the same namespace and throws `OccurrenceCommandNamespaceMismatch` when your tenant mapping disagrees. An occurrence without a namespace runs in the default namespace.

## Testing

Test an occurrence handler against the in-process kernel from `Cratis.Chronicle.Testing`: its capability switch makes the kernel advertise or hide `effect-receipts`, and it stores receipts so a second execution of the same effect key returns `AlreadyRecorded`. See [Testing with Chronicle](../../testing/chronicle.md#commands-for-occurrences).

## Things to know

- The effect name must be stable across attempts and unique per effect inside one occurrence. Pass `occurrence.EffectKey(name).Value` to external systems that accept an idempotency key.
- An effect needing more than one unit of work needs more than one effect name; each name has its own receipt.
- Commands whose results must carry personal data back to the occurrence are not a fit: the receipt is plaintext. Follow-up work for encrypted receipts is tracked separately.
- Event reactors that return commands are a different mechanism; see [command side effects](../reactors/command-side-effects.md).
