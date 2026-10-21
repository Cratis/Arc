---
title: Unique and once
description: Rules that a constraint enforces for every writer, with no read: a value that must be unique, and an event that may happen once.
---

Use this when the rule is that a value is used once, or that something happens once per entity. A constraint is the right tool. It needs no read and no read protection, and it holds for every writer.

| | |
| --- | --- |
| Rule | One library card per member. A username is unique across members |
| Mechanism | `[Unique]` on the event class (once per event source), or on a property (once per value) |
| Alternatives | An expected-empty scope for a one-off claim (Chronicle used directly). A new aggregate, which expects an empty history |
| Not | A read that asks "does it exist?" before the append. A decision read for value uniqueness: it guards one key's history, not a value across keys |

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[Unique(message: "This member already has a library card.")]
public record LibraryCardIssued();

[Command]
public record IssueLibraryCard(EventSourceId Member)
{
    public LibraryCardIssued Handle() => new();
}

[EventType]
public record UserRegistered([property: Unique(message: "That user name is taken.")] string UserName);

[Command]
public record RegisterUser(string UserName)
{
    public UserRegistered Handle() => new(UserName);
}
```

There is no guard line: the kernel checks the constraint inside the append, for every append.

## What holds

- A second `IssueLibraryCard` for the same member is rejected, whoever sends it and whatever the timing. Arc turns the violation into a validation error that carries your message.
- Two registrations of the same user name cannot both succeed. `RegisterUser` has no event source id property, so each call creates a new user stream, which is right for a creation command. The constraint, not the stream, makes the name unique.

## What does not hold

- A constraint is a fixed rule. If a user can change their name, release the old value with a removal event; see the [constraints](/chronicle/constraints/) documentation.
- Constraints across namespaces are not checked: tenants in different namespaces can claim the same value.

## Related

- [Pitfalls: a uniqueness check instead of a constraint](../pitfalls.md#a-uniqueness-check-instead-of-a-constraint)
- [Chronicle constraints](/chronicle/constraints/)
