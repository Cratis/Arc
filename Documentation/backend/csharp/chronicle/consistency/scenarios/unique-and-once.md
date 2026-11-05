---
title: Cratis Arc uniqueness and once-per-entity rules
description: Use Chronicle constraints in Cratis Arc to reserve a user name or issue one library card per member, without a read-before-write check.
---

Use a Chronicle constraint in Cratis Arc when a user name must belong to one user or a library card may be issued only once per member. The kernel checks the rule during append, so the command needs no read or read protection. The check applies to every append within the constraint's namespace and event sequence, regardless of which command writes it.

| | |
| --- | --- |
| Rule | One library card per member. A username is unique across members |
| Mechanism | `[Unique]` on the event class (once per event source), or on a property (a value owned by one event source) |
| Alternatives | For one claim on a known key: an expected-empty scope (Chronicle used directly), or a new aggregate that expects an empty history |
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

`[Unique]` has two different jobs here. On `LibraryCardIssued`, it limits that event type to one append per member. On `UserRegistered.UserName`, it reserves the name for the user that claims it. A read asking whether the card or name exists would leave a race between the answer and the append; the constraint checks at the append itself.

## What holds

- The first `IssueLibraryCard` for a member appends `LibraryCardIssued`. A second attempt is rejected with a validation error carrying `This member already has a library card.` and appends no second card event.
- Registration of an unclaimed user name appends `UserRegistered`. A competing registration of that name in the same namespace and event sequence is rejected with `That user name is taken.`

`RegisterUser` has no event source id property, so each call creates a new user stream. The name constraint must therefore span users; a decision read guarding one user's history would not protect this rule.

## What does not hold

A unique property reserves a value for an event source; it does not prohibit that source from appending the same value again. By default, claiming a different value replaces that source's previous claim and frees the old value. Use a removal event to release a claim explicitly. See [Chronicle constraints](/chronicle/constraints/) for retention and release options.

Class-level uniqueness remains once per event source, unless a removal event releases that constraint. Neither form spans namespaces or event sequences. Tenants in different namespaces can claim the same user name.

If your rule depends on a count or balance rather than uniqueness, use a [protected entity read](./single-entity.md) instead.

## Related

- [Pitfalls: a uniqueness check instead of a constraint](../pitfalls.md#a-uniqueness-check-instead-of-a-constraint)
- [Chronicle constraints](/chronicle/constraints/)
