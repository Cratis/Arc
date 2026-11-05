---
title: Arc aggregate roots for lifecycle rules
description: An Arc aggregate root keeps lifecycle rules together and guards its event source. Return Commit() so callers see validation and concurrency failures.
---

An aggregate root keeps an entity's state transitions and rules together as methods. Choose it in Cratis Arc when those rules are easier to express on the entity than as separate validators over a read model. Here, a book copy can be lent to only one member at a time.

| | |
| --- | --- |
| Rule | A copy of a book is lent to one member at a time, and more rules about the copy will follow |
| Mechanism | An aggregate root. Return the result of `Commit()` |
| Alternatives | A flat read model with a validator. Several validators over one model |
| Not | Ignoring the result of `Commit()`. A plain read of the same source in the same command |

```csharp
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

public record MemberId(Guid Value) : ConceptAs<Guid>(Value);

public record CopyId(string Value) : EventSourceId(Value);

[EventType]
public record CopyLent(MemberId Member);

[EventType]
public record CopyReturned();

public class Copy : AggregateRoot
{
    bool _lent;

    public async Task Lend(MemberId member)
    {
        if (_lent)
        {
            Failed("The copy is already lent.");
            return;
        }

        await Apply(new CopyLent(member));
    }

    public void OnCopyLent(CopyLent @event) => _lent = true;

    public void OnCopyReturned(CopyReturned @event) => _lent = false;
}

[Command]
public record LendCopy(CopyId Copy, MemberId Member)
{
    public async Task<AggregateRootCommitResult> Handle(Copy copy)
    {
        await copy.Lend(Member);
        return await copy.Commit();
    }
}
```

```text
guard copy: types=[CopyLent, CopyReturned], after <tail when the aggregate was resolved>
```

## Under read protection

| Situation | Under `On` |
| --- | --- |
| An aggregate and a plain read of a **different** source | Allowed. Each is its own guard |
| An aggregate and a plain read of the **same** source | Refused with `ReadAndAggregateOnSameSource`. Decide from the aggregate's state, or mark the read `[Unprotected("why")]`. [ARCCHR0033](../../code-analysis/ARCCHR0033.md) reports it at build time |
| `Commit()` returned last in the outermost command | Allowed. Arc commits through the owner of the unit |
| A read or an append after `Commit()` in the same command | Refused. Read first, return `Commit()` last. [ARCCHR0035](../../code-analysis/ARCCHR0035.md) reports it |
| `Commit()` in a nested command | Refused with `NestedEarlyCompletionUnderReadProtection`. Commit in the outer command |

Under `Off`, an aggregate cannot be combined with `[ProtectedDecision]` reads: `Commit()` throws once a decision read is enrolled.

## What holds

- An aggregate guards its own event source and the event types it handles. A new aggregate expects an empty history, so two commands that both create it cannot both succeed.
- `Failed()` prevents the commit when the copy is already lent. Returning the result of `Commit()` lets Arc report that rejection to the caller.

## What does not hold

- An aggregate does not guard a rule across aggregates. Two aggregates for two keys each guard only their own source. Use [two read models](./two-entities.md) for that.
- An aggregate has no snapshots, so every command reads the key's handled events again. The work grows with that history.
- A handler that fails after `Commit()` leaves its writes behind. That is the existing contract of an explicit commit.
- A handler that awaits `Commit()` and ignores its result lets a rejected command report success. See [pitfalls](../pitfalls.md#an-aggregate-commit-whose-result-is-ignored).

## Related

- [Aggregate roots in commands](../../aggregates/injecting-into-commands.md)
- [Defining an aggregate root](../../aggregates/defining-an-aggregate-root.md)
