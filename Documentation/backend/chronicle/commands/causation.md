---
title: Causation
description: A command's values are recorded on the causation of every event it appends, permanently — and how to keep a value out of that record.
---

Every event a command appends carries a causation chain saying how the work arrived: an HTTP request came in, a command ran, an aggregate root committed. Arc adds a link naming the command, and records **the values that command was asked to act on** alongside the name.

Naming the command alone answers "which command produced this event". Recording the values answers "which invocation" — two purchase orders raised by the same command are otherwise indistinguishable on the chain.

> [!IMPORTANT]
> The causation is written into the event log, and the event log is immutable. A value recorded there stays there for as long as the events do, is read by everything that ever replays them, and **cannot be taken back out by changing code**. Before adding a property to a command, decide whether its value belongs in a permanent audit record — and mark it [`[NotAudited]`](#keeping-a-value-out-of-the-record) if it does not.

## What gets recorded

For a command like this:

```csharp
[Command]
public record RaisePurchaseOrder(PurchaseOrderId OrderId, SupplierId Supplier, decimal Amount)
{
    public PurchaseOrderRaised Handle() => new(Supplier, Amount);
}
```

the causation link carries:

| Property | Value |
|---|---|
| `commandType` | `RaisePurchaseOrder` |
| `commandTypeFullName` | `Acme.Purchasing.RaisePurchaseOrder` |
| `eventSequenceId` | `event-log` |
| `orderId` | `00000026-0000-0000-0000-0000000000b2` |
| `supplier` | `ACME` |
| `amount` | `1234.56` |

Every readable public instance property is recorded, keyed by its camel-cased name. The values render as you would expect them to read:

- A [concept](/fundamentals/csharp/concepts/) records the value it wraps, not the wrapper — `orderId` above is the id, not `{"Value":"…"}`.
- Numbers and dates are written invariantly, dates round-trippably (`2026-02-26T11:03:00.0000000+00:00`), so a chain written in one locale reads the same in another.
- A nested object or a collection is written as compact JSON.
- A property that is not set is left out entirely, rather than recorded as empty.

## Keeping a value out of the record

Two markings keep a value off the chain.

### Personal data — `[PII]`

Anything Chronicle already treats as personal data is withheld automatically. Nothing extra is needed: mark it as you would anywhere else and it stays out of the causation as well as out of the event.

The marking is honored wherever it is written — on the property, on the command, on the positional parameter, and **on the concept**, so a concept marked once carries the marking to every command that uses it:

```csharp
[PII("The name of a person")]
public record ClaimantName(string Value) : ConceptAs<string>(Value);

[Command]
public record SubmitClaim(ClaimId ClaimId, ClaimantName Claimant);  // claimant is never recorded
```

See [Compliance](../compliance/pii.md) for the full picture.

### Secrets — `[NotAudited]`

A password, a token, an API key or a card number is not personal data, so `[PII]` does not describe it and would not keep it out. `[NotAudited]` does:

```csharp
[Command]
public record ChangePassword(
    UserId User,
    [property: NotAudited] string OldPassword,
    [property: NotAudited] string NewPassword)
{
    public PasswordChanged Handle(IPasswordHasher hasher) => new(hasher.Hash(NewPassword));
}
```

Written on a positional parameter — `[NotAudited] string OldPassword` — it works the same way.

Applied to a concept it travels with the value, the same way `[PII]` does — mark it once and every command that takes one is covered:

```csharp
[NotAudited]
public record ProviderApiKey(string Value) : ConceptAs<string>(Value);
```

Applied to the command itself it excludes every property at once, which is the right answer when a command exists only to carry secrets, and stays right as properties are added to it later:

```csharp
[Command]
[NotAudited]
public record ResetCredentials(UserId User, string Password, string RecoveryCode);
```

The command is still **named** on the chain either way. What is withheld is the values, never the fact that the command ran — an audit trail that hides which commands executed would not be an audit trail.

## The analyzer

[ARCCHR0009](../code-analysis/ARCCHR0009.md) reports a command property whose name reads like a secret and which is not marked. It is a name-based guess, which is normally a poor basis for a diagnostic — it earns its place here because the cost of a false positive is one attribute and the cost of a miss is a password in the event log forever.

It judges the type as well as the name, so a `DateTimeOffset` called `AccessTokenExpiresAt` is left alone — it is a timestamp, not a secret. When it is wrong the other way and a value that reads as a secret should be recorded, [suppress the diagnostic](../code-analysis/ARCCHR0009.md#when-the-rule-is-wrong) rather than marking it `[NotAudited]`, which would silence the warning by withholding a value you wanted.

It will not catch a secret whose name does not say so. A property called `Value` holding an API key is invisible to it, and to any reviewer reading the model. The analyzer narrows the problem; it does not remove your judgment from it.

## Size

A recorded value is cut short at 1024 characters, marked with `…` where it was cut. The causation travels on **every** event the command appends, so an unbounded value is written once per event — a value long enough to be truncated has stopped being an audit note and become a payload.

## Reading the chain

In the [Chronicle Workbench](/chronicle/workbench/), an event's **Context** tab shows its causation. Open `causation`, then an entry's `properties`, to see what the command was asked to do.

## See also

- [ARCCHR0009](../code-analysis/ARCCHR0009.md) — the analyzer for unmarked secrets
- [Compliance](../compliance/pii.md) — marking personal data
- [Events](events.md) — what a command returns
