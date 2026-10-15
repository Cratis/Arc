---
title: Gapless numbers
description: Numbers that have no gaps and no duplicates across all entities, written as a numbering source that is read in Provide.
---

Use this when a number must be unique and must not skip, across every entity: invoice numbers, case numbers.

| | |
| --- | --- |
| Rule | Invoice numbers have no gaps and no duplicates across all invoices |
| Mechanism | A numbering event source (`"invoice-numbers"`) with a projection of the next number, read with `IDecisionReads` in `Provide()`. The invoice carries the number |
| Alternatives | `[Unique]` on the number: no duplicates, but gaps are possible |
| Not | A declared `Concurrency` without an event source id: it is not anchored to a read |

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[EventType]
public record InvoiceNumberAssigned(string Invoice);

public record NextInvoiceNumber(
    EventSourceId Id,
    [Increment<InvoiceNumberAssigned>] int Assigned);

[EventType]
public record InvoiceIssued(int Number);

public record InvoiceId(string Value) : EventSourceId(Value);

[Command]
public record IssueInvoice(InvoiceId Invoice)
{
    public static readonly EventSourceId Numbering = "invoice-numbers";

    public async Task<NumberAssignment> Provide(IDecisionReads reads)
    {
        var read = await reads.Get<NextInvoiceNumber>(Numbering.Value);
        return new((read.Instance?.Assigned ?? 0) + 1);
    }

    public IEnumerable<EventForEventSourceId> Handle(NumberAssignment number) =>
        [
            new(Numbering, new InvoiceNumberAssigned(Invoice.Value)),
            new(Invoice, new InvoiceIssued(number.Number))
        ];
}

public record NumberAssignment(int Number);
```

```text
guard invoice-numbers: types=[InvoiceNumberAssigned], after <tail before the read>
```

The numbering source is read and then appended to in the same atomic batch. If another invoice took the next number between the read and the commit, nothing is written and the caller submits again. The batch also records `InvoiceIssued` on the invoice, so the number is on both sides.

## What holds

- No two invoices get the same number, and a number is not skipped, because every issue appends to the numbering source after reading it.
- A failed issue writes nothing, so no number is consumed.

## What does not hold

- A nested command that reads the numbering source after the outer command already staged `InvoiceNumberAssigned` is refused with `ReadOfSourceWithStagedEvents`: the read cannot see the staged event. Decide once in the outer command and pass the value down.
- All invoices serialize on the numbering source. That is the cost of a gapless sequence. If gaps are acceptable, use `[Unique]` on the number.

## Related

- [Reads](../reads.md)
- [Refusals](../refusals.md#other-reads-and-appends)
