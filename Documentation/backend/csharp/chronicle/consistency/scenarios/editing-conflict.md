---
title: Reject stale document edits in Arc
description: An explicit concurrency scope lets an Arc rename command reject a stale edit when the document title changed after the editor loaded it.
---

A captured concurrency scope lets Cratis Chronicle reject an edit made against an older document title. When saving through Cratis Arc, send the revision the editor loaded with the rename command. The scope guards the title changes since that revision.

| | |
| --- | --- |
| Rule | A rename is rejected if the title changed after the editor loaded it |
| Mechanism | An explicit scope built from the revision the client sent, returned with `EventsWithConcurrencyScopes` |
| Alternatives | A read model parameter when the conflict only matters within one request |
| Not | `concurrency: true` alone: it reads the tail after `Handle()`, so it cannot carry the revision the user saw |

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

public record DocumentId(string Value) : EventSourceId(Value);

[EventType]
public record DocumentTitleChanged(string Title);

[Command]
public record RenameDocument(DocumentId Document, string Title, ulong ExpectedRevision)
{
    public EventsWithConcurrencyScopes Handle() => new(
        [new(Document, new DocumentTitleChanged(Title))],
        [new(Document, new ConcurrencyScope(
            new EventSequenceNumber(ExpectedRevision),
            EventSourceId: Document,
            EventTypes: [typeof(DocumentTitleChanged).GetEventType()]))]);
}
```

```text
guard document: types=[DocumentTitleChanged], after <ExpectedRevision>
```

The scope fails the append if any `DocumentTitleChanged` for this document has a sequence number above `ExpectedRevision`. Your read model and query must supply that revision to the client. On a conflict, show the newer title and let the user decide what to save.

This example guards only title changes. To reject edits after other document changes, include the event types that represent those changes too.

## Under read protection

If the command also reads a model of the same document, the scope must cover what the read watches and must not have been captured after it. Otherwise the command is refused with `ExplicitScopeDoesNotCoverReads`. The scope above covers a read of `DocumentTitleChanged` events. A read model that folds more types than the scope names needs a wider scope. [ARCCHR0036](../../code-analysis/ARCCHR0036.md) reports it at build time.

## Related

- [Concurrency](../../commands/concurrency.md#carrying-the-revision-used-by-the-decision)
- [Chronicle: explicit scopes](/chronicle/events/concurrency/)
